using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Tests;

namespace UserSpaceShapingDemo.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class XdpForwarderBenchmarks : IDisposable
{
    private const int SenderPort = 5000;
    private const int ReceiverPort = 6000;

    private readonly TrafficSetup _directSetup;
    private readonly TrafficSetup _forwarderSetup1;
    private readonly TrafficSetup _forwarderSetup2;
    private readonly CancellationTokenSource _forwarderCancellation;
    private readonly Task _forwarderTask;
    private readonly Socket _directSender;
    private readonly Socket _directReceiver;
    private readonly Socket _forwardSender;
    private readonly Socket _forwardReceiver;
    private readonly SocketAddress _senderAddress = new IPEndPoint(TrafficSetup.SenderAddress, SenderPort).Serialize();
    private readonly SocketAddress _receiverAddress = new IPEndPoint(TrafficSetup.ReceiverAddress, ReceiverPort).Serialize();
    private readonly SocketAddress _addressBuffer = new IPEndPoint(IPAddress.Loopback, 0).Serialize();
    private readonly byte[] _packet = new byte[1500];
    private readonly byte[] _packetBuffer = new byte[1500];

    public XdpForwarderBenchmarks()
    {
        _directSetup = new();
        _forwarderSetup1 = new();
        _forwarderSetup2 = new(sharedSenderNs: _forwarderSetup1.ReceiverNs);
        _forwarderCancellation = new();
        _forwarderTask = Task.Factory.StartNew(() =>
        {
            using var forwardNs = _forwarderSetup1.EnterReceiver();
            try
            {
                XdpForwarder.Run(_forwarderSetup1.ReceiverName, _forwarderSetup2.SenderName, XdpForwarderMode.Generic, cancellationToken: _forwarderCancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }, TaskCreationOptions.LongRunning);
        _directSender = _directSetup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        _directReceiver = _directSetup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        _forwardSender = _forwarderSetup1.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        _forwardReceiver = _forwarderSetup2.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        RandomNumberGenerator.Fill(_packet);
    }

    public void Dispose()
    {
        _directSender.Dispose();
        _directReceiver.Dispose();
        _forwardSender.Dispose();
        _forwardReceiver.Dispose();
        _forwarderCancellation.Cancel();
        _forwarderTask.Wait();
        _forwarderTask.Dispose();
        _directSetup.Dispose();
        _forwarderSetup1.Dispose();
        _forwarderSetup2.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Send")]
    public void Send_Direct() => Send(_directSender, _directReceiver);

    [Benchmark]
    [BenchmarkCategory("Send")]
    public void Send_Forwarded() => Send(_forwardSender, _forwardReceiver);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Direct() => SendBatch(_directSender, _directReceiver);

    [Benchmark]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Forwarded() => SendBatch(_forwardSender, _forwardReceiver);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Direct() => SendFlow(_directSender, _directReceiver);

    [Benchmark]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Forwarded() => SendFlow(_forwardSender, _forwardReceiver);

    private void Send(Socket sender, Socket receiver)
    {
        sender.SendTo(_packet, SocketFlags.None, _receiverAddress);
        receiver.ReceiveFrom(_packetBuffer, SocketFlags.None, _addressBuffer);
    }

    private void SendBatch(Socket sender, Socket receiver)
    {
        const int batchSize = 16;
        for (var i = 0; i < batchSize; ++i)
            sender.SendTo(_packet, SocketFlags.None, _receiverAddress);
        for (var i = 0; i < batchSize; ++i)
            receiver.ReceiveFrom(_packetBuffer, SocketFlags.None, _addressBuffer);
    }

    private void SendFlow(Socket sender, Socket receiver)
    {
        const int flowSize = 1024;
        const int socketBufferSize = 16;
        var sendIndex = 0;
        var receiveIndex = 0;
        while (sendIndex < flowSize || receiveIndex < flowSize)
        {
            if (sendIndex++ < flowSize)
                sender.SendTo(_packet, SocketFlags.None, _receiverAddress);
            if (sendIndex >= socketBufferSize && receiveIndex++ < flowSize)
                receiver.ReceiveFrom(_packetBuffer, SocketFlags.None, _addressBuffer);
        }
    }
}