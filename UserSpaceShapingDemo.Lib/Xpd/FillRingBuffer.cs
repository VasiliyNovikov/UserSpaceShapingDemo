using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Fill(ReadOnlySpan<ulong> addresses)
    {
        var filledCount = 0u;
        var count = (uint)addresses.Length;
        ref var address = ref MemoryMarshal.GetReference(addresses);
        while (count > 0)
        {
            using var addressRange = Fill(count);
            if (addressRange.Length == 0)
                break;

            for (uint i = 0; i < addressRange.Length; ++i)
            {
                addressRange[i] = address;
                address = ref Unsafe.Add(ref address, 1);
            }
            count -= addressRange.Length;
            filledCount += addressRange.Length;
        }
        return filledCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AddressRange Fill(uint count)
    {
        count = Reserve(count, out var startIdx);
        return count == 0 ? default : new AddressRange(this, startIdx, count);
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct AddressRange(FillRingBuffer ringBuffer, uint index, uint length) : IDisposable
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
        public void Dispose()
        {
            if (length > 0)
                ringBuffer.Submit(length);
        }
    }
}