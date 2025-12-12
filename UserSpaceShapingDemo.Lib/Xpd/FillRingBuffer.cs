using System;
using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AddressRange Fill(uint count)
    {
        count = Reserve(count, out var startIdx);
        return count == 0 ? default : new AddressRange(this, startIdx, count);
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct AddressRange(FillRingBuffer ringBuffer, uint index, uint length)
    {
        public uint Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => length;
        }

        public ref ulong this[uint i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, length);
                return ref ringBuffer.Address(index + i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Submit(uint count)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, length);
            if (count > 0)
                ringBuffer.Submit(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Submit()
        {
            if (length > 0)
                ringBuffer.Submit(length);
        }
    }
}