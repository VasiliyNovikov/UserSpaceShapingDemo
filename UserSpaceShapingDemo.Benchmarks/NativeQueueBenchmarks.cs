using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using UserSpaceShapingDemo.Lib.Forwarding;

namespace UserSpaceShapingDemo.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class NativeQueueBenchmarks : IDisposable
{
    private const int BatchSize = 1000;

    private readonly Queue<long> _queue = new();
    private readonly ConcurrentQueue<long> _concurrentQueue = new();
    private readonly BlockingCollection<long> _blockingCollection = new();
    private readonly NativeQueue<long> _nativeQueue = new();

    public void Dispose()
    {
        _blockingCollection.Dispose();
        _nativeQueue.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("TryEmpty")]
    public bool Queue_TryDequeue_Empty() => _queue.TryDequeue(out _);

    [Benchmark, BenchmarkCategory("TryEmpty")]
    public bool ConcurrentQueue_TryDequeue_Empty() => _concurrentQueue.TryDequeue(out _);

    [Benchmark, BenchmarkCategory("TryEmpty")]
    public bool BlockingCollection_TryDequeue_Empty() => _blockingCollection.TryTake(out _);

    [Benchmark, BenchmarkCategory("TryEmpty")]
    public bool NativeQueue_TryDequeue_Empty() => _nativeQueue.TryDequeue(out _);

    [Benchmark(Baseline = true), BenchmarkCategory("One")]
    public long Queue_Enqueue_Dequeue_One()
    {
        _queue.Enqueue(0);
        return _queue.Dequeue();
    }

    [Benchmark, BenchmarkCategory("One")]
    public long BlockingCollection_Enqueue_Dequeue_One()
    {
        _blockingCollection.Add(0);
        return _blockingCollection.Take();
    }

    [Benchmark, BenchmarkCategory("One")]
    public long NativeQueue_Enqueue_Dequeue_One()
    {
        _nativeQueue.Enqueue(0);
        return _nativeQueue.Dequeue();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("TryOne")]
    public long Queue_Enqueue_TryDequeue_One()
    {
        _queue.Enqueue(0);
        _queue.TryDequeue(out var result);
        return result;
    }

    [Benchmark, BenchmarkCategory("TryOne")]
    public long ConcurrentQueue_Enqueue_TryDequeue_One()
    {
        _concurrentQueue.Enqueue(0);
        _concurrentQueue.TryDequeue(out var result);
        return result;
    }

    [Benchmark, BenchmarkCategory("TryOne")]
    public long BlockingCollection_Enqueue_TryDequeue_One()
    {
        _blockingCollection.Add(0);
        _blockingCollection.TryTake(out var result);
        return result;
    }

    [Benchmark, BenchmarkCategory("TryOne")]
    public long NativeQueue_Enqueue_TryDequeue_One()
    {
        _nativeQueue.Enqueue(0);
        _nativeQueue.TryDequeue(out var result);
        return result;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Batch")]
    public long Queue_Enqueue_Dequeue_Batch()
    {
        for (var i = 0; i < BatchSize; i++)
            _queue.Enqueue(i);
        long result = 0;
        for (var i = 0; i < BatchSize; i++)
            result += _queue.Dequeue();
        return result;
    }

    [Benchmark, BenchmarkCategory("Batch")]
    public long BlockingCollection_Enqueue_Dequeue_Batch()
    {
        for (var i = 0; i < BatchSize; i++)
            _blockingCollection.Add(i);
        long result = 0;
        for (var i = 0; i < BatchSize; i++)
            result += _blockingCollection.Take();
        return result;
    }

    [Benchmark, BenchmarkCategory("Batch")]
    public long NativeQueue_Enqueue_Dequeue_Batch()
    {
        for (var i = 0; i < BatchSize; i++)
            _nativeQueue.Enqueue(i);
        long result = 0;
        for (var i = 0; i < BatchSize; i++)
            result += _nativeQueue.Dequeue();
        return result;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("TryBatch")]
    public long Queue_Enqueue_TryDequeue_Batch()
    {
        for (var i = 0; i < BatchSize; i++)
            _queue.Enqueue(i);
        long result = 0;
        while (_queue.TryDequeue(out var value))
            result += value;
        return result;
    }

    [Benchmark, BenchmarkCategory("TryBatch")]
    public long ConcurrentQueue_Enqueue_TryDequeue_Batch()
    {
        for (var i = 0; i < BatchSize; i++)
            _concurrentQueue.Enqueue(i);
        long result = 0;
        while (_concurrentQueue.TryDequeue(out var value))
            result += value;
        return result;
    }

    [Benchmark, BenchmarkCategory("TryBatch")]
    public long BlockingCollection_Enqueue_TryDequeue_Batch()
    {
        for (var i = 0; i < BatchSize; i++)
            _blockingCollection.Add(i);
        long result = 0;
        while (_blockingCollection.TryTake(out var value))
            result += value;
        return result;
    }

    [Benchmark, BenchmarkCategory("TryBatch")]
    public long NativeQueue_Enqueue_TryDequeue_Batch()
    {
        for (var i = 0; i < BatchSize; i++)
            _nativeQueue.Enqueue(i);
        long result = 0;
        while (_nativeQueue.TryDequeue(out var value))
            result += value;
        return result;
    }
}