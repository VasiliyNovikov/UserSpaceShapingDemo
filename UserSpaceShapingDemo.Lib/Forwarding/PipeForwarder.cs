using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Headers;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class PipeForwarder : IDisposable
{
    private readonly PacketCallback? _receivedCallback;
    private readonly PacketCallback? _sentCallback;
    private readonly Action<Exception>? _errorCallback;
    private const int BatchSize = 64;

    private readonly XdpSocket _socket;
    private readonly NativeQueue<ulong> _freeFrames;
    private readonly NativeQueue<XdpDescriptor> _incomingPackets;
    private readonly NativeQueue<XdpDescriptor> _outgoingPackets;
    private readonly Worker _forwardingWorker;

    public PipeForwarder(ForwardingChannel channel, ForwardingChannel.Pipe pipe, uint queueId = 0, bool shared = false, PacketCallback? receivedCallback = null, PacketCallback? sentCallback = null, Action<Exception>? errorCallback = null)
    {
        _receivedCallback = receivedCallback;
        _sentCallback = sentCallback;
        _errorCallback = errorCallback;
        var socketMode = channel.Mode is ForwardingMode.Generic ? XdpSocketMode.Default : XdpSocketMode.Driver;
        var bindMode = channel.Mode is ForwardingMode.DriverZeroCopy ? XdpSocketBindMode.ZeroCopy : XdpSocketBindMode.Copy;
        _socket = new XdpSocket(channel.Memory, pipe.IfName, queueId, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup, shared: shared);
        _freeFrames = channel.FreeFrames;
        _incomingPackets = pipe.IncomingPackets;
        _outgoingPackets = pipe.OutgoingPackets;
        while (FillBatch()) ;
        _forwardingWorker = new(Run);
    }

    public void Dispose()
    {
        _forwardingWorker.Dispose();
        _socket.Dispose();
    }

    private void Run(CancellationToken cancellationToken)
    {
        try
        {
            List<IFileObject> waitObjects = [];
            List<Poll.Event> waitEvents = [];

            using var nativeCancellationToken = new NativeCancellationToken(cancellationToken);

            while (true)
            {
                while (ForwardBatch())
                    cancellationToken.ThrowIfCancellationRequested();

                waitObjects.Clear();
                waitEvents.Clear();

                waitObjects.Add(_socket);
                waitEvents.Add(_incomingPackets.Count > 0
                    ? Poll.Event.Readable
                    : Poll.Event.Readable | Poll.Event.Writable);
                if (_incomingPackets.Count == 0)
                {
                    waitObjects.Add(_incomingPackets);
                    waitEvents.Add(Poll.Event.Readable);
                }

                if (_freeFrames.Count == 0)
                {
                    waitObjects.Add(_freeFrames);
                    waitEvents.Add(Poll.Event.Readable);
                }

                nativeCancellationToken.Wait(CollectionsMarshal.AsSpan(waitObjects),
                    CollectionsMarshal.AsSpan(waitEvents));
            }
        }
        catch (Exception e)
        {
            _errorCallback?.Invoke(e);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ForwardBatch() => ReceiveBatch() | SendBatch() | CompleteBatch() | FillBatch();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReceiveBatch()
    {
        var receivePackets = _socket.RxRing.Receive(BatchSize);
        for (var i = 0u; i < receivePackets.Length; ++i)
        {
            var packet = receivePackets[i];
            _outgoingPackets.Enqueue(packet);
            _receivedCallback?.Invoke(_socket.IfName, _socket.Umem[packet]);
        }
        receivePackets.Release();
        return receivePackets.Length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SendBatch()
    {
        var sendPackets = _socket.TxRing.Send(BatchSize);
        var sendCount = 0u;
        while (sendCount < sendPackets.Length && _incomingPackets.TryDequeue(out var descriptor))
        {
            var packetData = _socket.Umem[descriptor];
            UpdateChecksums(packetData);
            sendPackets[sendCount++] = descriptor;
            _sentCallback?.Invoke(_socket.IfName, packetData);
        }

        if (sendCount == 0)
            return false;

        sendPackets.Submit(sendCount);
        if (_socket.TxRing.NeedsWakeup)
            _socket.WakeUp();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CompleteBatch()
    {
        var completed = _socket.CompletionRing.Complete();
        for (var i = 0u; i < completed.Length; ++i)
            _freeFrames.Enqueue(completed[i]);
        completed.Release();
        return completed.Length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool FillBatch()
    {
        var fill = _socket.FillRing.Fill(BatchSize);
        if (fill.Length == 0)
            return false;

        var filled = 0u;
        while (filled < fill.Length && _freeFrames.TryDequeue(out var frame))
            fill[filled++] = frame;
        if (filled == 0)
            return false;

        fill.Submit(filled);
        if (_socket.FillRing.NeedsWakeup)
            _socket.WakeUp();
        return true;
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