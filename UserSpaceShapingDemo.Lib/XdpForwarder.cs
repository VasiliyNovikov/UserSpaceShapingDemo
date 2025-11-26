using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Headers;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib;

public enum XdpForwarderMode
{
    Generic,
    Driver,
    DriverZeroCopy,
}

public static class XdpForwarder
{
    public delegate void PacketCallback(string eth, Span<byte> data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(string eth1, string eth2, XdpForwarderMode mode = XdpForwarderMode.Generic, PacketCallback? receivedCallback = null, PacketCallback? sentCallback = null, CancellationToken cancellationToken = default)
    {
        using var umem = new UMemory(UMemory.DefaultFrameCount * 2);

        var socketMode = mode is XdpForwarderMode.Generic ? XdpSocketMode.Default : XdpSocketMode.Driver;
        var bindMode = mode is XdpForwarderMode.DriverZeroCopy ? XdpSocketBindMode.ZeroCopy : XdpSocketBindMode.Copy;

        using var socket1 = new XdpSocket(umem, eth1, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup);
        using var socket2 = new XdpSocket(umem, eth2, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup, shared: true);

        Queue<XdpDescriptor> packetsToSend1 = [];
        Queue<XdpDescriptor> packetsToSend2 = [];
        Queue<ulong> freeAddresses = [];

        Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
        umem.GetAddresses(addresses);
        foreach (var address in addresses)
            freeAddresses.Enqueue(address);

        FillOnce(socket1, freeAddresses);
        FillOnce(socket2, freeAddresses);

        using var nativeCancellationToken = new NativeCancellationToken(cancellationToken);
        while (true)
        {
            while (ForwardOnce(socket1, socket2, packetsToSend2, freeAddresses, receivedCallback, sentCallback) |
                   ForwardOnce(socket2, socket1, packetsToSend1, freeAddresses, receivedCallback, sentCallback)) ;

            var events1 = Poll.Event.Readable;
            if (packetsToSend1.Count > 0)
                events1 |= Poll.Event.Writable;
            var events2 = Poll.Event.Readable;
            if (packetsToSend2.Count > 0)
                events2 |= Poll.Event.Writable;
            XdpSocket.WaitFor([socket1, socket2], [events1, events2], nativeCancellationToken);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ForwardOnce(XdpSocket sourceSocket, XdpSocket destinationSocket, Queue<XdpDescriptor> packetsToSend, Queue<ulong> freeAddresses, PacketCallback? receivedCallback, PacketCallback? sentCallback)
    {
        var hasActivity = false;
        using (var receivePackets = sourceSocket.RxRing.Receive())
        {
            for (var i = 0u; i < receivePackets.Length; ++i)
            {
                hasActivity = true;
                ref readonly var packet = ref receivePackets[i];
                packetsToSend.Enqueue(packet);
                var packetData = sourceSocket.Umem[packet];
                UpdateChecksums(packetData);
                receivedCallback?.Invoke(sourceSocket.IfName, packetData);
            }
        }

        using (var sendPackets = destinationSocket.TxRing.Send((uint)packetsToSend.Count))
        {
            for (var i = 0u; i < sendPackets.Length; ++i)
            {
                hasActivity = true;
                var descriptor = packetsToSend.Dequeue();
                ref var packet = ref sendPackets[i];
                packet.Length = descriptor.Length;
                packet.Address = descriptor.Address;
                sentCallback?.Invoke(destinationSocket.IfName, destinationSocket.Umem[packet]);
            }
        }

        if (destinationSocket.TxRing.NeedsWakeup)
            destinationSocket.WakeUp();

        using (var completed = destinationSocket.CompletionRing.Complete())
            for (var i = 0u; i < completed.Length; ++i)
            {
                hasActivity = true;
                freeAddresses.Enqueue(completed[i]);
            }

        if (FillOnce(sourceSocket, freeAddresses))
            hasActivity = true;

        return hasActivity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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