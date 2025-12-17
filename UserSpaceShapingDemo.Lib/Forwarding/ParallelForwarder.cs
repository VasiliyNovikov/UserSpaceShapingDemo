using System;
using System.Collections.Generic;

using UserSpaceShapingDemo.Lib.Links;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class ParallelForwarder : IDisposable
{
    private readonly ForwardingChannel _channel;
    private readonly List<PipeForwarder> _forwarders = new();

    public ParallelForwarder(string ifName1, string ifName2, ForwardingMode mode = ForwardingMode.Generic, IForwardingLogger? logger = null)
    {
        _channel = new ForwardingChannel(ifName1, ifName2, mode);
        var shared = false;
        foreach (var pipe in new [] { _channel.Pipe1, _channel.Pipe2 })
        {
            using var collection = new LinkCollection();
            var rxQueueCount = collection[pipe.IfName].RXQueueCount;
            var txQueueCount = collection[pipe.IfName].TXQueueCount;
            var queueCount = Math.Max(rxQueueCount, txQueueCount);
            for (var queueId = 0u; queueId < queueCount; ++queueId)
            {
                _forwarders.Add(new PipeForwarder(_channel, pipe, queueId, queueId < rxQueueCount, queueId < txQueueCount, shared, logger));
                shared = true;
            }
        }
    }

    public void Dispose()
    {
        foreach (var forwarder in _forwarders)
            forwarder.Dispose();
        _channel.Dispose();
    }
}