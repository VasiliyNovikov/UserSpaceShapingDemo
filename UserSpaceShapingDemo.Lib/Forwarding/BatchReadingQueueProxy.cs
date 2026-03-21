using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using LinuxCore;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class BatchReadingQueueProxy<T>(NativeQueue<T> queue, int batchSize) : IFileObject
{
    private readonly T[] _buffer = new T[batchSize];
    private int _offset;

    public FileDescriptor Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => queue.Descriptor;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LocalCount == 0 && queue.IsEmpty;
    }

    public int LocalCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(ReadOnlySpan<T> items) => queue.Enqueue(items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FetchLocal()
    {
        if (LocalCount == 0)
        {
            _offset = 0;
            LocalCount = queue.TryDequeue(_buffer);
        }
        return LocalCount > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeueLocal([MaybeNullWhen(false)] out T item)
    {
        if (LocalCount == 0)
        {
            Unsafe.SkipInit(out item);
            return false;
        }
        --LocalCount;
        item = _buffer[_offset++];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T DequeueLocal() => TryDequeueLocal(out var item) ? item : throw new InvalidOperationException("No local items available. Invoke FetchLocal() first.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue() => TryDequeueLocal(out var item) || FetchLocal() && TryDequeueLocal(out item) ? item : queue.Dequeue();
}