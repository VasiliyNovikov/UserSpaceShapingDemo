using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class CompletionRingBuffer : ConsumerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AddressRange Complete(uint count)
    {
        count = Peek(count, out var startIdx);
        return count == 0 ? default : new AddressRange(this, startIdx, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AddressRange Complete()
    {
        var count = Peek(out var startIdx);
        return count == 0 ? default : new AddressRange(this, startIdx, count);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Complete(Span<ulong> frames)
    {
        var count = Peek((uint)frames.Length, out var startIdx);
        ref var frame = ref MemoryMarshal.GetReference(frames);
        for (var i = 0u; i < count; ++i)
        {
            frame = Address(startIdx++);
            frame = ref Unsafe.Add(ref frame, 1);
        }
        Release(count);
        return count;
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct AddressRange(CompletionRingBuffer ringBuffer, uint index, uint length)
    {
        public uint Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => length;
        }

        public ref readonly ulong this[uint i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, length);
                return ref ringBuffer.Address(index + i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            if (length > 0)
                ringBuffer.Release(length);
        }
    }
}