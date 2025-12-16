using System;
using System.Runtime.CompilerServices;

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