using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

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

    private static readonly TrafficSetup DirectSetup = new();
    private static readonly TrafficForwardingSetup ForwardingGenericSetup = new(errorCallback: e => Console.Error.WriteLine(e));
    private static readonly TrafficForwardingSetup ForwardingDriverSetup = new(XdpForwarderMode.Driver, errorCallback: e => Console.Error.WriteLine(e));
    private static readonly Socket DirectSender;
    private static readonly Socket DirectReceiver;
    private static readonly Socket ForwardingGenericSender;
    private static readonly Socket ForwardingGenericReceiver;
    private static readonly Socket ForwardingDriverSender;
    private static readonly Socket ForwardingDriverReceiver;
    private static readonly SocketAddress ReceiverAddress = new IPEndPoint(TrafficSetup.ReceiverAddress, ReceiverPort).Serialize();
    private static readonly SocketAddress AddressBuffer = new IPEndPoint(IPAddress.Loopback, 0).Serialize();
    private static readonly byte[] Packet = new byte[1500];
    private static readonly byte[] PacketBuffer = new byte[1500];

    static XdpForwarderBenchmarks()
    {
        DirectSender = DirectSetup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        DirectReceiver = DirectSetup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        ForwardingGenericSender = ForwardingGenericSetup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        ForwardingGenericReceiver = ForwardingGenericSetup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        ForwardingDriverSender = ForwardingDriverSetup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, SenderPort);
        ForwardingDriverReceiver = ForwardingDriverSetup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, ReceiverPort);
        RandomNumberGenerator.Fill(Packet);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Send")]
    public void Send_Direct() => Send(DirectSender, DirectReceiver);

    [Benchmark]
    [BenchmarkCategory("Send")]
    public void Send_Forwarded_Generic() => Send(ForwardingGenericSender, ForwardingGenericReceiver);

    [Benchmark]
    [BenchmarkCategory("Send")]
    public void Send_Forwarded_Driver() => Send(ForwardingDriverSender, ForwardingDriverReceiver);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Direct() => SendBatch(DirectSender, DirectReceiver);

    [Benchmark]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Forwarded_Generic() => SendBatch(ForwardingGenericSender, ForwardingGenericReceiver);

    [Benchmark]
    [BenchmarkCategory("SendBatch")]
    public void SendBatch_Forwarded_Driver() => SendBatch(ForwardingDriverSender, ForwardingDriverReceiver);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Direct() => SendFlow(DirectSender, DirectReceiver);

    [Benchmark]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Forwarded_Generic() => SendFlow(ForwardingGenericSender, ForwardingGenericReceiver);

    [Benchmark]
    [BenchmarkCategory("SendFlow")]
    public void SendFlow_Forwarded_Driver() => SendFlow(ForwardingDriverSender, ForwardingDriverReceiver);

    private static void Send(Socket sender, Socket receiver)
    {
        sender.SendTo(Packet, SocketFlags.None, ReceiverAddress);
        receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, AddressBuffer);
    }

    private static void SendBatch(Socket sender, Socket receiver)
    {
        const int batchSize = 16;
        for (var i = 0; i < batchSize; ++i)
            sender.SendTo(Packet, SocketFlags.None, ReceiverAddress);
        for (var i = 0; i < batchSize; ++i)
            receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, AddressBuffer);
    }

    private static void SendFlow(Socket sender, Socket receiver)
    {
        const int flowSize = 1024;
        const int socketBufferSize = 16;
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