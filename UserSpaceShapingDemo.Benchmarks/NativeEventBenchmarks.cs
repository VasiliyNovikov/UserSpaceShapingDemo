using System.Threading;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using LinuxCore;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class NativeEventBenchmarks
{
    private static readonly LinuxEvent Event = new(false);

    [Benchmark]
    public void Set()
    {
        Event.Set();
    }

    [Benchmark]
    public void Set_Wait()
    {
        Event.Set();
        Event.Wait();
    }

    [Benchmark]
    public void Set_Poll_Wait()
    {
        Event.Set();
        LinuxPoll.Wait(Event.Descriptor, LinuxPoll.Event.Readable, Timeout.InfiniteTimeSpan);
        Event.Wait();
    }
}