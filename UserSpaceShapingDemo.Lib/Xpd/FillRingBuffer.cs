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
            var n = Reserve(count, out var idx);
            if (n == 0)
                break;

            var lastIdx = idx + n;
            for (var i = idx; i < lastIdx; ++i)
            {
                Address(i) = address;
                address = ref Unsafe.Add(ref address, 1);
            }
            Submit(n);
            count -= n;
            filledCount += n;
        }
        return filledCount;
    }
}