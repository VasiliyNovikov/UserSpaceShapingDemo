using System;
using System.Net.Sockets;

using UserSpaceShapingDemo.Lib.Forwarding;

namespace UserSpaceShapingDemo.Tests;

public sealed class TrafficForwardingSetup : IDisposable
{
    private readonly TrafficSetup _setup1;
    private readonly TrafficSetup _setup2;
    private readonly IDisposable _forwarder;

    public TrafficForwardingSetup(TrafficForwarderType forwarderType = TrafficForwarderType.Simple, ForwardingMode mode = ForwardingMode.Generic,
                                  string? sharedForwarderNs = null, byte rxQueueCount = 1, byte txQueueCount = 1, IForwardingLogger? logger = null)
    {
        _setup1 = new TrafficSetup(sharedReceiverNs: sharedForwarderNs, rxQueueCount: rxQueueCount, txQueueCount: txQueueCount);
        _setup2 = new TrafficSetup(sharedSenderNs: _setup1.ReceiverNs, rxQueueCount: rxQueueCount, txQueueCount: txQueueCount);
        using (_setup1.EnterReceiver())
        {
            _forwarder = forwarderType == TrafficForwarderType.Simple
                ? new SimpleForwarder(_setup1.ReceiverName, _setup2.SenderName, mode, logger)
                : new ParallelForwarder(_setup1.ReceiverName, _setup2.SenderName, mode, logger);
        }
    }

    public void Dispose()
    {
        _forwarder.Dispose();
        _setup2.Dispose();
        _setup1.Dispose();
    }

    public Socket CreateSenderSocket(int version, ProtocolType protocolType, int port = 0) => _setup1.CreateSenderSocket(version, protocolType, port);
    public Socket CreateReceiverSocket(int version, ProtocolType protocolType, int port) => _setup2.CreateReceiverSocket(version, protocolType, port);
}