using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Tests;

namespace UserSpaceShapingDemo.Benchmarks;

public sealed class DirectBenchmark : TrafficBenchmark, IDisposable
{
    private readonly TrafficSetup _setup;

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

    public DirectBenchmark(int version) : base(version)
    {
        _setup = new TrafficSetup();
        Sender = _setup.CreateSenderSocket(4, ProtocolType.Udp, SenderPort);
        Receiver = _setup.CreateReceiverSocket(4, ProtocolType.Udp, ReceiverPort);
    }

    public void Dispose()
    {
        Sender.Dispose();
        Receiver.Dispose();
        _setup.Dispose();
    }
}