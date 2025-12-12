using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class NativeQueue<T> : IFileObject, IDisposable
{
    private readonly NativeSemaphore _counter = new();
    private readonly ConcurrentQueue<T> _queue = new();

    public int Count => _queue.Count;

    public FileDescriptor Descriptor => _counter.Descriptor;

    public void Dispose() => _counter.Dispose();

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        _counter.Increment();
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        if (!_queue.TryDequeue(out item))
            return false;
        _counter.Decrement();
        return true;
    }
}