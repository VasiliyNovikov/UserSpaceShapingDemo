using System;
using System.Collections.Generic;

using UserSpaceShapingDemo.Lib.Links;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class ParallelForwarder : IDisposable
{
    private readonly ForwardingChannel _channel;
    private readonly List<PipeForwarder> _forwarders = new();

    public ParallelForwarder(string ifName1, string ifName2, ForwardingMode mode = ForwardingMode.Generic,
                             PacketCallback? receivedCallback = null, PacketCallback? sentCallback = null, Action<Exception>? errorCallback = null)
    {
        _channel = new ForwardingChannel(ifName1, ifName2, mode);
        var shared = false;
        foreach (var pipe in new [] { _channel.Pipe1, _channel.Pipe2 })
        {
            using var collection = new LinkCollection();
            var rxQueueCount = collection[pipe.IfName].RxQueueCount;
            for (var queueId = 0u; queueId < rxQueueCount; ++queueId)
            {
                _forwarders.Add(new PipeForwarder(_channel, pipe, queueId, shared, receivedCallback, sentCallback, errorCallback));
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