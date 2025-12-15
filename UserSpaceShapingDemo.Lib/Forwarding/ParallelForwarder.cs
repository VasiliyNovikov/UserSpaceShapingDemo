using System;
using System.Collections.Generic;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class ParallelForwarder : IDisposable
{
    private readonly ForwardingChannel _channel;
    private readonly List<PipeForwarder> _forwarders = new();

    public ParallelForwarder(string ifName1, string ifName2, ForwardingMode mode = ForwardingMode.Generic, uint ifQueueCount = 1)
    {
        _channel = new ForwardingChannel(ifName1, ifName2, mode);
        var shared = false;
        for (var i = 0u; i < ifQueueCount; ++i)
        {
            _forwarders.Add(new PipeForwarder(_channel, _channel.Pipe1, i, shared));
            shared = true;
            _forwarders.Add(new PipeForwarder(_channel, _channel.Pipe2, i, shared));
        }
    }

    public void Dispose()
    {
        foreach (var forwarder in _forwarders)
            forwarder.Dispose();
        _channel.Dispose();
    }
}