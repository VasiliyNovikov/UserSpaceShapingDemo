using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Tests;

namespace UserSpaceShapingDemo.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class XdpForwarderBenchmarks
{
    private const int SenderPort = 5000;
    private const int ReceiverPort = 6000;

    private static readonly TrafficSetup DirectSetup;
    private static readonly TrafficSetup ForwarderGenericSetup1;
    private static readonly TrafficSetup ForwarderGenericSetup2;
    private static readonly TrafficSetup ForwarderDriverSetup1;
    private static readonly TrafficSetup ForwarderDriverSetup2;
    private static readonly Socket DirectSender;
    private static readonly Socket DirectReceiver;
    private static readonly Socket ForwardGenericSender;
    private static readonly Socket ForwardGenericReceiver;
    private static readonly Socket ForwardDriverSender;
    private static readonly Socket ForwardDriverReceiver;
    private static readonly SocketAddress SenderAddress = new IPEndPoint(TrafficSetup.SenderAddress, SenderPort).Serialize();
    private static readonly SocketAddress ReceiverAddress = new IPEndPoint(TrafficSetup.ReceiverAddress, ReceiverPort).Serialize();
    private static readonly SocketAddress AddressBuffer = new IPEndPoint(IPAddress.Loopback, 0).Serialize();
    private static readonly byte[] Packet = new byte[1500];
    private static readonly byte[] PacketBuffer = new byte[1500];

    static  XdpForwarderBenchmarks()
    {
        DirectSetup = new();
        ForwarderGenericSetup1 = new();
        ForwarderGenericSetup2 = new(sharedSenderNs: ForwarderGenericSetup1.ReceiverNs);
        Task.Factory.StartNew(() =>
        {
            using var forwardNs = ForwarderGenericSetup1.EnterReceiver();
            try
            {
                XdpForwarder.Run(ForwarderGenericSetup1.ReceiverName, ForwarderGenericSetup2.SenderName, XdpForwarderMode.Generic);
            }
            catch (OperationCanceledException)
            {
            }
        }, TaskCreationOptions.LongRunning);
        ForwarderDriverSetup1 = new();
        ForwarderDriverSetup2 = new(sharedSenderNs: ForwarderDriverSetup1.ReceiverNs);
        Task.Factory.StartNew(() =>
        {
            using var forwardNs = ForwarderDriverSetup1.EnterReceiver();
            try
            {
                XdpForwarder.Run(ForwarderDriverSetup1.ReceiverName, ForwarderDriverSetup2.SenderName, XdpForwarderMode.Generic);
            }
            catch (OperationCanceledException)
            {
            }
        }, TaskCreationOptions.LongRunning);
        DirectSender = DirectSetup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        DirectReceiver = DirectSetup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        ForwardGenericSender = ForwarderGenericSetup1.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        ForwardGenericReceiver = ForwarderGenericSetup2.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        ForwardDriverSender = ForwarderDriverSetup1.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        ForwardDriverReceiver = ForwarderDriverSetup2.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        RandomNumberGenerator.Fill(Packet);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Send")]
    public void Send_Direct() => Send(DirectSender, DirectReceiver);

    [Benchmark]
    [BenchmarkCategory("Send")]
    public void Send_Forwarded_Generic() => Send(ForwardGenericSender, ForwardGenericReceiver);

    [Benchmark]
    [BenchmarkCategory("Send")]
    public void Send_Forwarded_Driver() => Send(ForwardDriverSender, ForwardDriverReceiver);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Direct() => SendBatch(DirectSender, DirectReceiver);

    [Benchmark]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Forwarded_Generic() => SendBatch(ForwardGenericSender, ForwardGenericReceiver);

    [Benchmark]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Forwarded_Driver() => SendBatch(ForwardDriverSender, ForwardDriverReceiver);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Direct() => SendFlow(DirectSender, DirectReceiver);

    [Benchmark]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Forwarded_Generic() => SendFlow(ForwardGenericSender, ForwardGenericReceiver);

    [Benchmark]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Forwarded_Driver() => SendFlow(ForwardDriverSender, ForwardDriverReceiver);

    private void Send(Socket sender, Socket receiver)
    {
        sender.SendTo(Packet, SocketFlags.None, ReceiverAddress);
        receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, AddressBuffer);
    }

    private void SendBatch(Socket sender, Socket receiver)
    {
        const int batchSize = 15;
        for (var i = 0; i < batchSize; ++i)
            sender.SendTo(Packet, SocketFlags.None, ReceiverAddress);
        for (var i = 0; i < batchSize; ++i)
            receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, AddressBuffer);
    }

    private void SendFlow(Socket sender, Socket receiver)
    {
        const int flowSize = 1024;
        const int socketBufferSize = 15;
        var sendIndex = 0;
        var receiveIndex = 0;
        while (sendIndex < flowSize || receiveIndex < flowSize)
        {
            if (sendIndex++ < flowSize)
                sender.SendTo(Packet, SocketFlags.None, ReceiverAddress);
            if (sendIndex >= socketBufferSize && receiveIndex++ < flowSize)
                receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, AddressBuffer);
        }
    }
}