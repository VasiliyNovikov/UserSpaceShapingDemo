using System;
using System.Threading;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using UserSpaceShapingDemo.Lib.Forwarding;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;
using UserSpaceShapingDemo.Tests;

namespace UserSpaceShapingDemo.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class ForwardingBenchmarks
{
    private static readonly string? ForwarderNs = XdpSocket.IsLegacyApi ? "fw-bench" : null;
    private static readonly DirectBenchmark Direct4 = new(4);
    private static readonly DirectBenchmark Direct6 = new(6);
    private static readonly ForwardingBenchmark ForwardingSimpleGeneric4;
    private static readonly ForwardingBenchmark ForwardingSimpleGeneric6;
    private static readonly ForwardingBenchmark ForwardingParallelGeneric4Q1;
    private static readonly ForwardingBenchmark ForwardingParallelGeneric6Q1;
    private static readonly ForwardingBenchmark ForwardingParallelGeneric4Q3;
    private static readonly ForwardingBenchmark ForwardingParallelGeneric6Q3;

    static ForwardingBenchmarks()
    {
        XdpLogger.SetLogger((level, message) =>
        {
            if (level <= XdpLogLevel.Information)
                Console.Error.WriteLine($"{DateTime.UtcNow:O}: [XDP {level}] {message}");
        });

        if (ForwarderNs is not null)
            NetNs.ReCreate(ForwarderNs);

        ForwardingSimpleGeneric4 = new(4, TrafficForwarderType.Simple, ForwardingMode.Generic, ForwarderNs, 1, 1);
        Thread.Sleep(100);
        ForwardingSimpleGeneric6 = new(6, TrafficForwarderType.Simple, ForwardingMode.Generic, ForwarderNs, 1, 1);
        Thread.Sleep(100);
        ForwardingParallelGeneric4Q1 = new(4, TrafficForwarderType.Parallel, ForwardingMode.Generic, ForwarderNs, 1, 1);
        Thread.Sleep(100);
        ForwardingParallelGeneric6Q1 = new(6, TrafficForwarderType.Parallel, ForwardingMode.Generic, ForwarderNs, 1, 1);
        Thread.Sleep(100);
        ForwardingParallelGeneric4Q3 = new(4, TrafficForwarderType.Parallel, ForwardingMode.Generic, ForwarderNs, 3, 3);
        Thread.Sleep(100);
        ForwardingParallelGeneric6Q3 = new(6, TrafficForwarderType.Parallel, ForwardingMode.Generic, ForwarderNs, 3, 3);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Send", "IPv4")]
    public void Send_Direct4() => Direct4.SendOne();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Send", "IPv6")]
    public void Send_Direct6() => Direct6.SendOne();

    [Benchmark]
    [BenchmarkCategory("Send", "IPv4")]
    public void Send_Forwarded_Simple_Generic4() => ForwardingSimpleGeneric4.SendOne();

    [Benchmark]
    [BenchmarkCategory("Send", "IPv6")]
    public void Send_Forwarded_Simple_Generic6() => ForwardingSimpleGeneric6.SendOne();

    [Benchmark]
    [BenchmarkCategory("Send", "IPv4")]
    public void Send_Forwarded_Parallel_Generic4Q1() => ForwardingParallelGeneric4Q1.SendOne();

    [Benchmark]
    [BenchmarkCategory("Send", "IPv6")]
    public void Send_Forwarded_Parallel_Generic6Q1() => ForwardingParallelGeneric6Q1.SendOne();

    [Benchmark]
    [BenchmarkCategory("Send", "IPv4")]
    public void Send_Forwarded_Parallel_Generic4Q3() => ForwardingParallelGeneric4Q3.SendOne();

    [Benchmark]
    [BenchmarkCategory("Send", "IPv6")]
    public void Send_Forwarded_Parallel_Generic6Q3() => ForwardingParallelGeneric6Q3.SendOne();


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendBatch", "IPv4")]
    public void SendBatch_Direct() => Direct4.SendBatch();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendBatch", "IPv6")]
    public void SendBatch_Direct6() => Direct6.SendBatch();

    [Benchmark]
    [BenchmarkCategory("SendBatch", "IPv4")]
    public void SendBatch_Forwarded_Simple_Generic4() => ForwardingSimpleGeneric4.SendBatch();

    [Benchmark]
    [BenchmarkCategory("SendBatch", "IPv6")]
    public void SendBatch_Forwarded_Simple_Generic6() => ForwardingSimpleGeneric6.SendBatch();

    [Benchmark]
    [BenchmarkCategory("SendBatch", "IPv4")]
    public void SendBatch_Forwarded_Parallel_Generic4Q1() => ForwardingParallelGeneric4Q1.SendBatch();

    [Benchmark]
    [BenchmarkCategory("SendBatch", "IPv6")]
    public void SendBatch_Forwarded_Parallel_Generic6Q1() => ForwardingParallelGeneric6Q1.SendBatch();

    [Benchmark]
    [BenchmarkCategory("SendBatch", "IPv4")]
    public void SendBatch_Forwarded_Parallel_Generic4Q3() => ForwardingParallelGeneric4Q3.SendBatch();

    [Benchmark]
    [BenchmarkCategory("SendBatch", "IPv6")]
    public void SendBatch_Forwarded_Parallel_Generic6Q3() => ForwardingParallelGeneric6Q3.SendBatch();


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendFlow", "IPv4")]
    public void SendFlow_Direct() => Direct4.SendFlow();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendFlow", "IPv6")]
    public void SendFlow_Direct6() => Direct6.SendFlow();

    [Benchmark]
    [BenchmarkCategory("SendFlow", "IPv4")]
    public void SendFlow_Forwarded_Simple_Generic4() => ForwardingSimpleGeneric4.SendFlow();

    [Benchmark]
    [BenchmarkCategory("SendFlow", "IPv6")]
    public void SendFlow_Forwarded_Simple_Generic6() => ForwardingSimpleGeneric6.SendFlow();

    [Benchmark]
    [BenchmarkCategory("SendFlow", "IPv4")]
    public void SendFlow_Forwarded_Parallel_Generic4Q1() => ForwardingParallelGeneric4Q1.SendFlow();

    [Benchmark]
    [BenchmarkCategory("SendFlow", "IPv6")]
    public void SendFlow_Forwarded_Parallel_Generic6Q1() => ForwardingParallelGeneric6Q1.SendFlow();

    [Benchmark]
    [BenchmarkCategory("SendFlow", "IPv4")]
    public void SendFlow_Forwarded_Parallel_Generic4Q3() => ForwardingParallelGeneric4Q3.SendFlow();

    [Benchmark]
    [BenchmarkCategory("SendFlow", "IPv6")]
    public void SendFlow_Forwarded_Parallel_Generic6Q3() => ForwardingParallelGeneric6Q3.SendFlow();
}