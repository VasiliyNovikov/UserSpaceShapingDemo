using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using LinuxCore;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class NativeQueue<T> : IFileObject, IDisposable
{
    private readonly LinuxSemaphoreSlim _counter = new();
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
    public void Enqueue(ReadOnlySpan<T> items)
    {
        foreach (var item in items)
            _queue.Enqueue(item);
        _counter.Add((uint)items.Length);
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
    public int TryDequeue(Span<T> items)
    {
        var count = 0;
        while (count < items.Length && _queue.TryDequeue(out var item))
            items[count++] = item;
        _counter.Remove((uint)count);
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        T? item;
        while (!TryDequeue(out item))
            LinuxPoll.Wait(_counter.Descriptor, LinuxPoll.Event.Readable, Timeout.Infinite);
        return item;
    }
}