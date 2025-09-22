using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed unsafe class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref ulong Address(uint idx) => ref base.Address(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void Init(XdpSocket socket)
    {
        var umem = socket.Umem;
        Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
        ref var address = ref MemoryMarshal.GetReference(addresses);
        for (var i = 0; i < umem.FrameCount; ++i)
        {
            address = (ulong)i * umem.FrameSize;
            address = ref Unsafe.Add(ref address, 1);
        }
        while (!addresses.IsEmpty)
        {
            addresses = addresses[(int)Fill(addresses)..];
            if (!addresses.IsEmpty)
                Thread.Yield();
        }
    }

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