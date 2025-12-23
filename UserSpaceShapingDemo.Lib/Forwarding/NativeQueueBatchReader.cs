using System;
using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class NativeQueueBatchReader<T>(NativeQueue<T> queue, int batchSize)
{
    private readonly T[] _buffer = new T[batchSize];
    private int _offset;
    private int _count;

    public NativeQueue<T> Queue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => queue;
    }

    public int LocalCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    public bool FetchLocal()
    {
        if (_count == 0)
        {
            _offset = 0;
            _count = queue.TryDequeue(_buffer);
        }
        return _count > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T DequeueLocal()
    {
        if (_count == 0)
            throw new InvalidOperationException("No local items available. Call FetchLocal() first.");
        --_count;
        return _buffer[_offset++];
    }
}