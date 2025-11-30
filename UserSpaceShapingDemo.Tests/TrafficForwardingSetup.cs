using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using UserSpaceShapingDemo.Lib;

namespace UserSpaceShapingDemo.Tests;

public sealed class TrafficForwardingSetup : IDisposable
{
    private readonly TrafficSetup _setup1;
    private readonly TrafficSetup _setup2;
    private readonly Task _forwardingTask;
    private readonly CancellationTokenSource _forwardingCancellation;

    public TrafficForwardingSetup(XdpForwarderMode mode = XdpForwarderMode.Generic,
                                  XdpForwarder.PacketCallback? receivedCallback = null, XdpForwarder.PacketCallback? sentCallback = null,
                                  Action<Exception>? errorCallback = null)
    {
        _setup1 = new TrafficSetup();
        _setup2 = new TrafficSetup(sharedSenderNs: _setup1.ReceiverNs);
        _forwardingCancellation = new();
        _forwardingTask = Task.Factory.StartNew(() =>
        {
            
            using var forwardNs = _setup1.EnterReceiver();
            try
            {
                XdpForwarder.Run(_setup1.ReceiverName, _setup2.SenderName, mode, receivedCallback, sentCallback, errorCallback, _forwardingCancellation.Token);
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
        _setup2.Dispose();
        _setup1.Dispose();
    }

    public Socket CreateSenderSocket(SocketType socketType, ProtocolType protocolType, int port = 0) => _setup1.CreateSenderSocket(socketType, protocolType, port);
    public Socket CreateReceiverSocket(SocketType socketType, ProtocolType protocolType, int port) => _setup2.CreateReceiverSocket(socketType, protocolType, port);
}