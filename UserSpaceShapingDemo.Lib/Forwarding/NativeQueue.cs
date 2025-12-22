using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class NativeQueue<T> : IFileObject, IDisposable
{
    private readonly NativeSemaphore _counter = new();
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
        using var _ = HangDebugHelper.Measure("NativeQueue.Enqueue");
        _queue.Enqueue(item);
        _counter.Increment();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        using var _ = HangDebugHelper.Measure("NativeQueue.TryDequeue");
        if (!_queue.TryDequeue(out item))
            return false;
        _counter.Decrement();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        using var _ = HangDebugHelper.Measure("NativeQueue.Dequeue");
        _counter.Decrement();
        return _queue.TryDequeue(out var item) ? item : throw new InvalidOperationException("Queue is empty");
    }
}