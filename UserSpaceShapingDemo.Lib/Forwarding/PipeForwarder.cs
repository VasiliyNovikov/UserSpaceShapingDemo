using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class PipeForwarder : IDisposable
{
    private const int FillBatchSize = 32;
    private const int SendBatchSize = 32;

    private readonly bool _canReceive;
    private readonly bool _canSend;
    private readonly IForwardingLogger? _logger;
    private readonly XdpSocket _socket;
    private readonly NativeQueue<ulong> _freeFrames;
    private readonly Queue<ulong> _freeFramesLocal = new();
    private readonly NativeQueue<XdpDescriptor> _incomingPackets;
    private readonly Queue<XdpDescriptor> _incomingPacketsLocal = new();
    private readonly NativeQueue<XdpDescriptor> _outgoingPackets;
    private readonly Worker _forwardingWorker;

    public PipeForwarder(ForwardingChannel channel, ForwardingChannel.Pipe pipe, uint queueId, bool canReceive, bool canSend, bool shared, IForwardingLogger? logger)
    {
        _canReceive = canReceive;
        _canSend = canSend;
        _logger = logger;
        var socketMode = channel.Mode is ForwardingMode.Generic ? XdpSocketMode.Default : XdpSocketMode.Driver;
        var bindMode = channel.Mode is ForwardingMode.DriverZeroCopy ? XdpSocketBindMode.ZeroCopy : XdpSocketBindMode.Copy;
        _socket = new XdpSocket(channel.Memory, pipe.IfName, queueId, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup, shared: shared);
        _freeFrames = channel.FreeFrames;
        _incomingPackets = pipe.IncomingPackets;
        _outgoingPackets = pipe.OutgoingPackets;
        if (canReceive)
        {
            var fill = _socket.FillRing.Fill(_socket.FillRing.Capacity);
            for (var i = 0u; i < fill.Length; i++)
                fill[i] = _freeFrames.Dequeue();
            fill.Submit();
            _logger?.Log(_socket.IfName, _socket.QueueId, $"Initially filled {fill.Length} frames");
        }
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

                using (HangDebugHelper.Measure("PipeForwarder.Run -> ForwardBatch loop"))
                    while (ForwardBatch())
                        cancellationToken.ThrowIfCancellationRequested();

                waitObjects.Clear();
                waitEvents.Clear();

                var socketEvents = _canReceive ? Poll.Event.Readable : Poll.Event.None;
                if (_canSend && !_incomingPackets.IsEmpty)
                    socketEvents |= Poll.Event.Writable;
                if (socketEvents != Poll.Event.None)
                {
                    waitObjects.Add(_socket);
                    waitEvents.Add(socketEvents);
                }

                if (_canSend)
                {
                    waitObjects.Add(_incomingPackets);
                    waitEvents.Add(Poll.Event.Readable);
                }

                if (_canReceive && _freeFrames.IsEmpty)
                {
                    waitObjects.Add(_freeFrames);
                    waitEvents.Add(Poll.Event.Readable);
                }

                using (HangDebugHelper.Measure("PipeForwarder.Run -> Wait", 1_000_000_000))
                {
                    _logger?.Log(_socket.IfName, _socket.QueueId, "Entering the poll");
                    nativeCancellationToken.Wait(CollectionsMarshal.AsSpan(waitObjects), CollectionsMarshal.AsSpan(waitEvents));
                    _logger?.Log(_socket.IfName, _socket.QueueId, "Woke up from poll");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger?.LogError("Forwarding loop failed", e);
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ForwardBatch()
    {
        using var _ = HangDebugHelper.Measure("PipeForwarder.ForwardBatch");
        var result = false;
        if (_canReceive)
            result = ReceiveBatch() | FillBatch();
        if (_canSend)
            result |= SendBatch() | CompleteBatch();
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReceiveBatch()
    {
        var receivePackets = _socket.RxRing.Receive();
        _logger?.Log(_socket.IfName, _socket.QueueId, $"Received {receivePackets.Length} packets");
        for (var i = 0u; i < receivePackets.Length; ++i)
        {
            var packet = receivePackets[i];
            _logger?.LogPacket(_socket.IfName, _socket.QueueId, "Received packet", _socket.Umem[packet]);
            _outgoingPackets.Enqueue(packet);
        }
        receivePackets.Release();
        return receivePackets.Length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SendBatch()
    {
        if (_incomingPacketsLocal.Count == 0)
            for (var i = 0; i < SendBatchSize && _incomingPackets.TryDequeue(out var packet); ++i)
                _incomingPacketsLocal.Enqueue(packet);

        var sendPackets = _socket.TxRing.Send((uint)_incomingPacketsLocal.Count);
        _logger?.Log(_socket.IfName, _socket.QueueId, $"Will send {sendPackets.Length} packets");
        for (var i = 0u; i < sendPackets.Length; ++i)
        {
            var packet = _incomingPacketsLocal.Dequeue();
            var packetData = _socket.Umem[packet];
            sendPackets[i] = packet;
            _logger?.LogPacket(_socket.IfName, _socket.QueueId, "Sent packet", packetData);
        }
        if (sendPackets.Length == 0)
            return false;

        sendPackets.Submit();
        if (_socket.TxRing.NeedsWakeup)
        {
            _logger?.Log(_socket.IfName, _socket.QueueId, "Waking up socket for TX");
            try
            {
                var error = _socket.WakeUp();
                _logger?.Log(_socket.IfName, _socket.QueueId, $"Woke up socket for TX: {error}");
            }
            catch (Exception e)
            {
                _logger?.LogError(_socket.IfName, _socket.QueueId, "Failed to wake up socket for TX", e);
                throw;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CompleteBatch()
    {
        var completed = _socket.CompletionRing.Complete();
        _logger?.Log(_socket.IfName, _socket.QueueId, $"Completed {completed.Length} frames");
        for (var i = 0u; i < completed.Length; ++i)
            _freeFrames.Enqueue(completed[i]);
        completed.Release();
        return completed.Length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool FillBatch()
    {
        if (_freeFramesLocal.Count == 0)
            for (var i = 0; i < FillBatchSize && _freeFrames.TryDequeue(out var frame); ++i)
                _freeFramesLocal.Enqueue(frame);

        var fill = _socket.FillRing.Fill((uint)_freeFramesLocal.Count);
        for (var i = 0u; i < fill.Length; i++)
            fill[i] = _freeFramesLocal.Dequeue();

        _logger?.Log(_socket.IfName, _socket.QueueId, $"Filled {fill.Length} frames");

        if (fill.Length == 0)
            return false;

        fill.Submit();
        if (_socket.FillRing.NeedsWakeup)
            _socket.WakeUp();
        return true;
    }
}