using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Forwarding;
using UserSpaceShapingDemo.Tests;

namespace UserSpaceShapingDemo.Benchmarks;

public sealed class ForwardingBenchmark : TrafficBenchmark, IDisposable
{
    private readonly TrafficForwardingSetup _setup;

    protected override Socket Sender
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    protected override Socket Receiver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    public ForwardingBenchmark(int version, TrafficForwarderType type, ForwardingMode mode, string? sharedForwarderNs, byte rxQueueCount, byte txQueueCount)
        : base(version)
    {
        _setup = new TrafficForwardingSetup(type, mode, sharedForwarderNs, rxQueueCount, txQueueCount);
        Sender = _setup.CreateSenderSocket(version, ProtocolType.Udp, SenderPort);
        Receiver = _setup.CreateReceiverSocket(version, ProtocolType.Udp, ReceiverPort);
    }

    public void Dispose()
    {
        Sender.Dispose();
        Receiver.Dispose();
        _setup.Dispose();
    }
}