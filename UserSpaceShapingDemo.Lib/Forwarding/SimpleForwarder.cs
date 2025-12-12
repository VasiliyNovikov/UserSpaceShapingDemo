using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Headers;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public static class SimpleForwarder
{
    public delegate void PacketCallback(string eth, Span<byte> data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(string eth1, string eth2, ForwardingMode mode = ForwardingMode.Generic,
                           PacketCallback? receivedCallback = null, PacketCallback? sentCallback = null,
                           Action<Exception>? errorCallback = null,
                           CancellationToken cancellationToken = default)
    {
        try
        {
            using var umem = new UMemory();

            var socketMode = mode is ForwardingMode.Generic ? XdpSocketMode.Default : XdpSocketMode.Driver;
            var bindMode = mode is ForwardingMode.DriverZeroCopy ? XdpSocketBindMode.ZeroCopy : XdpSocketBindMode.Copy;

            using var socket1 = new XdpSocket(umem, eth1, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup);
            using var socket2 = new XdpSocket(umem, eth2, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup, shared: true);

            Queue<XdpDescriptor> packetsToSend1 = [];
            Queue<XdpDescriptor> packetsToSend2 = [];
            Stack<ulong> freeAddresses = [];

            Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
            umem.GetAddresses(addresses);
            foreach (var address in addresses)
                freeAddresses.Push(address);

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
                nativeCancellationToken.Wait([socket1, socket2], [events1, events2]);
            }
        }
        catch (Exception ex)
        {
            errorCallback?.Invoke(ex);
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FillOnce(XdpSocket socket, Stack<ulong> freeAddresses)
    {
        var fill = socket.FillRing.Fill((uint)freeAddresses.Count);
        var filled = fill.Length > 0;
        for (var i = 0u; i < fill.Length; ++i)
            fill[i] = freeAddresses.Pop();
        fill.Submit();
        if (filled && socket.FillRing.NeedsWakeup)
            socket.WakeUp();
        return filled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ForwardOnce(XdpSocket sourceSocket, XdpSocket destinationSocket, Queue<XdpDescriptor> packetsToSend, Stack<ulong> freeAddresses, PacketCallback? receivedCallback, PacketCallback? sentCallback)
    {
        var receivePackets = sourceSocket.RxRing.Receive();
        var hasActivity = receivePackets.Length > 0;
        for (var i = 0u; i < receivePackets.Length; ++i)
        {
            var packet = receivePackets[i];
            packetsToSend.Enqueue(packet);
            var packetData = sourceSocket.Umem[packet];
            UpdateChecksums(packetData);
            receivedCallback?.Invoke(sourceSocket.IfName, packetData);
        }
        receivePackets.Release();

        var sendPackets = destinationSocket.TxRing.Send((uint)packetsToSend.Count);
        hasActivity |= sendPackets.Length > 0;
        for (var i = 0u; i < sendPackets.Length; ++i)
        {
            var descriptor = packetsToSend.Dequeue();
            sendPackets[i] = descriptor;
            sentCallback?.Invoke(destinationSocket.IfName, destinationSocket.Umem[descriptor]);
        }
        sendPackets.Submit();

        if (destinationSocket.TxRing.NeedsWakeup)
            destinationSocket.WakeUp();

        var completed = destinationSocket.CompletionRing.Complete();
        hasActivity |= completed.Length > 0;
        for (var i = 0u; i < completed.Length; ++i)
            freeAddresses.Push(completed[i]);
        completed.Release();

        hasActivity |= FillOnce(sourceSocket, freeAddresses);

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