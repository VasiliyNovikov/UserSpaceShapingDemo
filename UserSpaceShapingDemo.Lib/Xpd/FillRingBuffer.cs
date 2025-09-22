using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed unsafe class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref ulong Address(uint idx) => ref base.Address(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void InitFill(XdpSocket socket, UMemory umem)
    {
        Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
        ref var address = ref MemoryMarshal.GetReference(addresses);
        for (var i = 0; i < umem.FrameCount; ++i)
        {
            address = (ulong)i * umem.FrameSize;
            address = ref Unsafe.Add(ref address, 1);
        }
        FillAll(socket, addresses);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillAll(XdpSocket socket, ReadOnlySpan<ulong> addresses)
    {
        while (!addresses.IsEmpty)
        {
            addresses = addresses[(int)Fill(addresses)..];
            if (addresses.IsEmpty)
                break;
            Poll.Wait(socket.Descriptor, Poll.Event.Readable, 1);
        }
    }
}