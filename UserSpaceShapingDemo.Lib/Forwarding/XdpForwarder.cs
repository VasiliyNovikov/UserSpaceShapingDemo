using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using UserSpaceShapingDemo.Lib.Headers;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class XdpForwarder : IDisposable
{
    private const int BatchSize = 64;

    private readonly XdpSocket _socket;
    private readonly NativeQueue<ulong> _freeFrames;
    private readonly NativeQueue<XdpDescriptor> _incomingPackets;
    private readonly NativeQueue<XdpDescriptor> _outgoingPackets;
    private readonly Task _forwardingTask;
    private readonly CancellationTokenSource _forwardingCancellation;

    public XdpForwarder(ForwardingChannel channel, ForwardingChannel.Pipe pipe, bool shared = false)
    {
        _socket = new XdpSocket(channel.Memory, pipe.Eth, shared: shared);
        _freeFrames = channel.FreeFrames;
        _incomingPackets = pipe.IncomingPackets;
        _outgoingPackets = pipe.OutgoingPackets;
        while (FillBatch()) ;
        _forwardingCancellation = new();
        _forwardingTask = Task.Factory.StartNew(() =>
        {
            
            try
            {
                Run(_forwardingCancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }, TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        _forwardingCancellation.Cancel();
        _forwardingTask.Wait();
        _forwardingCancellation.Dispose();
        _forwardingTask.Dispose();
        _socket.Dispose();
    }

    private void Run(CancellationToken cancellationToken)
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
            waitEvents.Add(_incomingPackets.Count > 0 ? Poll.Event.Readable : Poll.Event.Readable | Poll.Event.Writable);
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

            nativeCancellationToken.Wait(CollectionsMarshal.AsSpan(waitObjects), CollectionsMarshal.AsSpan(waitEvents));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ForwardBatch() => ReceiveBatch() | SendBatch() | CompleteBatch() | FillBatch();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReceiveBatch()
    {
        var receivePackets = _socket.RxRing.Receive(BatchSize);
        for (var i = 0u; i < receivePackets.Length; ++i)
            _outgoingPackets.Enqueue(receivePackets[i]);
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