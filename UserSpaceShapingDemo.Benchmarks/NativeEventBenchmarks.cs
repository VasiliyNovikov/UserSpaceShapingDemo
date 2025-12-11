using System.Threading;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class NativeEventBenchmarks
{
    private static readonly NativeEvent Event = new(false);

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
        Poll.Wait(Event.Descriptor, Poll.Event.Readable, Timeout.InfiniteTimeSpan);
        Event.Wait();
    }
}