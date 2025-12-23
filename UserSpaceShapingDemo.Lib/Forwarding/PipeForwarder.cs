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
    private const int ReceiveBatchSize = 32;
    private const int CompleteBatchSize = 32;

    private readonly bool _canReceive;
    private readonly bool _canSend;
    private readonly IForwardingLogger? _logger;
    private readonly XdpSocket _socket;
    private readonly NativeQueueBatchReader<ulong> _freeFrames;
    private readonly NativeQueueBatchReader<XdpDescriptor> _incomingPackets;
    private readonly NativeQueue<XdpDescriptor> _outgoingPackets;
    private readonly Worker _forwardingWorker;
    private readonly XdpDescriptor[] _receiveBuffer = new XdpDescriptor[ReceiveBatchSize];
    private readonly ulong[] _completeBuffer = new ulong[CompleteBatchSize];

    public PipeForwarder(ForwardingChannel channel, ForwardingChannel.Pipe pipe, uint queueId, bool canReceive, bool canSend, bool shared, IForwardingLogger? logger)
    {
        _canReceive = canReceive;
        _canSend = canSend;
        _logger = logger;
        var socketMode = channel.Mode is ForwardingMode.Generic ? XdpSocketMode.Default : XdpSocketMode.Driver;
        var bindMode = channel.Mode is ForwardingMode.DriverZeroCopy ? XdpSocketBindMode.ZeroCopy : XdpSocketBindMode.Copy;
        _socket = new XdpSocket(channel.Memory, pipe.IfName, queueId, mode: socketMode, bindMode: bindMode | XdpSocketBindMode.UseNeedWakeup, shared: shared);
        _freeFrames = new(channel.FreeFrames, FillBatchSize);
        _incomingPackets = new(pipe.IncomingPackets, SendBatchSize);
        _outgoingPackets = pipe.OutgoingPackets;
        if (canReceive)
        {
            var fill = _socket.FillRing.Fill(_socket.FillRing.Capacity);
            for (var i = 0u; i < fill.Length; i++)
                fill[i] = _freeFrames.Queue.Dequeue();
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

                while (ForwardBatch())
                    cancellationToken.ThrowIfCancellationRequested();

                if (ForwardBatch())
                    continue;

                if (ForwardBatch())
                    continue;

                if (ForwardBatch())
                    continue;

                Thread.SpinWait(1);

                if (ForwardBatch())
                    continue;

                Thread.SpinWait(10);

                if (ForwardBatch())
                    continue;

                Thread.SpinWait(100);

                if (ForwardBatch())
                    continue;

                waitObjects.Clear();
                waitEvents.Clear();

                var socketEvents = _canReceive ? Poll.Event.Readable : Poll.Event.None;
                if (_canSend && !_incomingPackets.Queue.IsEmpty)
                    socketEvents |= Poll.Event.Writable;
                if (socketEvents != Poll.Event.None)
                {
                    waitObjects.Add(_socket);
                    waitEvents.Add(socketEvents);
                }

                if (_canSend)
                {
                    waitObjects.Add(_incomingPackets.Queue);
                    waitEvents.Add(Poll.Event.Readable);
                }

                if (_canReceive && _freeFrames.Queue.IsEmpty)
                {
                    waitObjects.Add(_freeFrames.Queue);
                    waitEvents.Add(Poll.Event.Readable);
                }

                _logger?.Log(_socket.IfName, _socket.QueueId, "Entering the poll");
                nativeCancellationToken.Wait(CollectionsMarshal.AsSpan(waitObjects), CollectionsMarshal.AsSpan(waitEvents));
                _logger?.Log(_socket.IfName, _socket.QueueId, "Woke up from poll");
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
        var packets = _receiveBuffer.AsSpan(0, (int)_socket.RxRing.Receive(_receiveBuffer));
        if (_logger is { } logger)
        {
            logger.Log(_socket.IfName, _socket.QueueId, $"Received {packets.Length} packets");
            foreach (var packet in packets)
                logger.LogPacket(_socket.IfName, _socket.QueueId, "Received packet", _socket.Umem[packet]);
        }
        _outgoingPackets.Enqueue(packets);
        return packets.Length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SendBatch()
    {
        if (!_incomingPackets.FetchLocal())
            return false;

        var sendPackets = _socket.TxRing.Send((uint)_incomingPackets.LocalCount);
        _logger?.Log(_socket.IfName, _socket.QueueId, $"Will send {sendPackets.Length} packets");
        for (var i = 0u; i < sendPackets.Length; ++i)
        {
            var packet = _incomingPackets.DequeueLocal();
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
        var frames = _completeBuffer.AsSpan(0, (int)_socket.CompletionRing.Complete(_completeBuffer));
        _logger?.Log(_socket.IfName, _socket.QueueId, $"Completed {frames.Length} frames");
        _freeFrames.Queue.Enqueue(frames);
        return !frames.IsEmpty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool FillBatch()
    {
        if (!_freeFrames.FetchLocal())
            return false;

        var fill = _socket.FillRing.Fill((uint)_freeFrames.LocalCount);
        for (var i = 0u; i < fill.Length; i++)
            fill[i] = _freeFrames.DequeueLocal();

        _logger?.Log(_socket.IfName, _socket.QueueId, $"Filled {fill.Length} frames");

        if (fill.Length == 0)
            return false;

        fill.Submit();
        if (_socket.FillRing.NeedsWakeup)
            _socket.WakeUp();
        return true;
    }
}