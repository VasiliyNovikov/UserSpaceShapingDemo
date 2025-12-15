using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Headers;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class SimpleForwarder : IDisposable
{
    public delegate void PacketCallback(string eth, Span<byte> data);

    private readonly PacketCallback? _receivedCallback;
    private readonly PacketCallback? _sentCallback;
    private readonly Action<Exception>? _errorCallback;
    private readonly UMemory _umem;
    private readonly XdpSocket _socket1;
    private readonly XdpSocket _socket2;
    private readonly Worker _forwardingWorker;

    public SimpleForwarder(string ifName1, string ifName2, ForwardingMode mode = ForwardingMode.Generic,
                           PacketCallback? receivedCallback = null, PacketCallback? sentCallback = null, Action<Exception>? errorCallback = null)
    {
        _receivedCallback = receivedCallback;
        _sentCallback = sentCallback;
        _errorCallback = errorCallback;
        var socketMode = mode is ForwardingMode.Generic ? XdpSocketMode.Default : XdpSocketMode.Driver;
        var bindMode = mode is ForwardingMode.DriverZeroCopy ? XdpSocketBindMode.ZeroCopy : XdpSocketBindMode.Copy;
        _umem = new UMemory();
        _socket1 = new XdpSocket(_umem, ifName1, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup);
        _socket2 = new XdpSocket(_umem, ifName2, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup, shared: true);
        _forwardingWorker = new Worker(Run);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Run(CancellationToken cancellationToken)
    {
        try
        {
            Queue<XdpDescriptor> packetsToSend1 = [];
            Queue<XdpDescriptor> packetsToSend2 = [];
            Stack<ulong> freeAddresses = [];

            Span<ulong> addresses = stackalloc ulong[(int)_umem.FrameCount];
            _umem.GetAddresses(addresses);
            foreach (var address in addresses)
                freeAddresses.Push(address);

            FillOnce(_socket1, freeAddresses);
            FillOnce(_socket2, freeAddresses);

            using var nativeCancellationToken = new NativeCancellationToken(cancellationToken);
            while (true)
            {
                while (ForwardOnce(_socket1, _socket2, packetsToSend2, freeAddresses) |
                       ForwardOnce(_socket2, _socket1, packetsToSend1, freeAddresses)) ;

                var events1 = Poll.Event.Readable;
                if (packetsToSend1.Count > 0)
                    events1 |= Poll.Event.Writable;
                var events2 = Poll.Event.Readable;
                if (packetsToSend2.Count > 0)
                    events2 |= Poll.Event.Writable;
                nativeCancellationToken.Wait([_socket1, _socket2], [events1, events2]);
            }
        }
        catch (Exception ex)
        {
            _errorCallback?.Invoke(ex);
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
    private bool ForwardOnce(XdpSocket sourceSocket, XdpSocket destinationSocket, Queue<XdpDescriptor> packetsToSend, Stack<ulong> freeAddresses)
    {
        var receivePackets = sourceSocket.RxRing.Receive();
        var hasActivity = receivePackets.Length > 0;
        for (var i = 0u; i < receivePackets.Length; ++i)
        {
            var packet = receivePackets[i];
            packetsToSend.Enqueue(packet);
            var packetData = sourceSocket.Umem[packet];
            UpdateChecksums(packetData);
            _receivedCallback?.Invoke(sourceSocket.IfName, packetData);
        }
        receivePackets.Release();

        var sendPackets = destinationSocket.TxRing.Send((uint)packetsToSend.Count);
        hasActivity |= sendPackets.Length > 0;
        for (var i = 0u; i < sendPackets.Length; ++i)
        {
            var descriptor = packetsToSend.Dequeue();
            sendPackets[i] = descriptor;
            _sentCallback?.Invoke(destinationSocket.IfName, destinationSocket.Umem[descriptor]);
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

    public void Dispose()
    {
        _forwardingWorker.Dispose();
        _socket1.Dispose();
        _socket2.Dispose();
        _umem.Dispose();
    }
}