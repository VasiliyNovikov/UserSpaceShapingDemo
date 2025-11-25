using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Headers;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib;

public static class XdpForwarder
{
    public delegate void PacketCallback(string eth, Span<byte> data);

    public static void Run(string eth1, string eth2, PacketCallback? receivedCallback = null, PacketCallback? sentCallback = null, CancellationToken cancellationToken = default)
    {
        using var umem = new UMemory();
        Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
        umem.GetAddresses(addresses);

        using var socket1 = new XdpSocket(umem, eth1);
        using var socket2 = new XdpSocket(umem, eth2, shared: true);
        
        Queue<XdpDescriptor> packetsToSend1 = [];
        Queue<XdpDescriptor> packetsToSend2 = [];
        Queue<ulong> addressesToFill1 = [];
        Queue<ulong> addressesToFill2 = [];

        foreach(var address in addresses[..(addresses.Length / 2)])
            addressesToFill1.Enqueue(address);
        foreach(var address in addresses[(addresses.Length / 2)..])
            addressesToFill2.Enqueue(address);

        FillOnce(socket1, addressesToFill1);
        FillOnce(socket2, addressesToFill2);

        using var nativeCancellationToken = new NativeCancellationToken(cancellationToken);
        while (true)
        {
            while (ForwardOnce(socket1, socket2, packetsToSend2, addressesToFill1, receivedCallback, sentCallback) |
                   ForwardOnce(socket2, socket1, packetsToSend1, addressesToFill2, receivedCallback, sentCallback)) ;

            var events1 = Poll.Event.Readable;
            if (packetsToSend1.Count > 0)
                events1 |= Poll.Event.Writable;
            var events2 = Poll.Event.Readable;
            if (packetsToSend2.Count > 0)
                events2 |= Poll.Event.Writable;
            XdpSocket.WaitFor([socket1, socket2], [events1, events2], nativeCancellationToken);
        }
    }

    private static bool FillOnce(XdpSocket socket, Queue<ulong> addressesToFill)
    {
        var filled = false;
        using var fill = socket.FillRing.Fill((uint)addressesToFill.Count);
        for (var i = 0u; i < fill.Length; ++i)
        {
            filled = true;
            fill[i] = addressesToFill.Dequeue();
        }
        return filled;
    }

    private static bool ForwardOnce(XdpSocket sourceSocket, XdpSocket destinationSocket, Queue<XdpDescriptor> packetsToSend, Queue<ulong> addressesToFill, PacketCallback? receivedCallback, PacketCallback? sentCallback)
    {
        var hasActivity = false;
        using (var receivePackets = sourceSocket.RxRing.Receive())
        {
            for (var i = 0u; i < receivePackets.Length; ++i)
            {
                hasActivity = true;
                ref readonly var packet = ref receivePackets[i];
                packetsToSend.Enqueue(packet);
                receivedCallback?.Invoke(sourceSocket.IfName, sourceSocket.Umem[packet]);
            }
        }

        using (var sendPackets = destinationSocket.TxRing.Send((uint)packetsToSend.Count))
        {
            for (var i = 0u; i < sendPackets.Length; ++i)
            {
                hasActivity = true;
                var descriptor = packetsToSend.Dequeue();
                ref var packet = ref sendPackets[i];
                packet.Address = descriptor.Address;
                packet.Length = descriptor.Length;
                var packetData = sourceSocket.Umem[descriptor];
                UpdateChecksums(packetData);
                sentCallback?.Invoke(destinationSocket.IfName, packetData);
            }
        }

        if (destinationSocket.TxRing.NeedsWakeup)
            destinationSocket.WakeUp();

        using (var completed = destinationSocket.CompletionRing.Complete())
            for (var i = 0u; i < completed.Length; ++i)
            {
                hasActivity = true;
                addressesToFill.Enqueue(completed[i]);
            }

        if (FillOnce(sourceSocket, addressesToFill))
            hasActivity = true;

        return hasActivity;
    }

    private static void UpdateChecksums(Span<byte> packetData)
    {
        ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
        if (ethernetHeader.EtherType == EthernetType.IPv4)
        {
            ref var ipv4Header = ref ethernetHeader.Layer2Header<IPv4Header>();
            if (ipv4Header.Protocol == IPProtocol.UDP)
                ipv4Header.Layer3Header<UDPHeader>().Checksum = default;
        }
    }
}