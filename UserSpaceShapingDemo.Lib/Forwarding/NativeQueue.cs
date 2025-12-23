using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class NativeQueue<T> : IFileObject, IDisposable
{
    private readonly NativeSemaphoreSlim _counter = new();
    private readonly ConcurrentQueue<T> _queue = new();

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _queue.IsEmpty;
    }

    public FileDescriptor Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _counter.Descriptor;
    }

    public void Dispose() => _counter.Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        _counter.Increment();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        if (!_queue.TryDequeue(out item))
            return false;
        _counter.Decrement();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        T? item;
        while (!TryDequeue(out item))
            Poll.Wait(_counter.Descriptor, Poll.Event.Readable, Timeout.Infinite);
        return item;
    }
}