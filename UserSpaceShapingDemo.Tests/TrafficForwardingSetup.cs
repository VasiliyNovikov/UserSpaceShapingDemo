using System;
using System.Net.Sockets;

using UserSpaceShapingDemo.Lib.Forwarding;

namespace UserSpaceShapingDemo.Tests;

public sealed class TrafficForwardingSetup : IDisposable
{
    private readonly TrafficSetup _setup1;
    private readonly TrafficSetup _setup2;
    private readonly SimpleForwarder _forwarder;

    public TrafficForwardingSetup(ForwardingMode mode = ForwardingMode.Generic,
                                  string? sharedForwarderNs = null, 
                                  SimpleForwarder.PacketCallback? receivedCallback = null,
                                  SimpleForwarder.PacketCallback? sentCallback = null,
                                  Action<Exception>? errorCallback = null)
    {
        _setup1 = new TrafficSetup(sharedReceiverNs: sharedForwarderNs);
        _setup2 = new TrafficSetup(sharedSenderNs: _setup1.ReceiverNs);
        using (_setup1.EnterReceiver())
            _forwarder = new SimpleForwarder(_setup1.ReceiverName, _setup2.SenderName, mode, receivedCallback, sentCallback, errorCallback);
    }

    public void Dispose()
    {
        _forwarder.Dispose();
        _setup2.Dispose();
        _setup1.Dispose();
    }

    public Socket CreateSenderSocket(SocketType socketType, ProtocolType protocolType, int port = 0) => _setup1.CreateSenderSocket(socketType, protocolType, port);
    public Socket CreateReceiverSocket(SocketType socketType, ProtocolType protocolType, int port) => _setup2.CreateReceiverSocket(socketType, protocolType, port);
}