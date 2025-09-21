using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed unsafe class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref ulong Address(uint idx) => ref base.Address(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void InitFill(UMemory umem, XdpSocket? socket = null)
    {
        Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
        ref var address = ref MemoryMarshal.GetReference(addresses);
        for (var i = 0; i < umem.FrameCount; ++i)
        {
            address = (ulong)i * umem.FrameSize;
            address = ref Unsafe.Add(ref address, 1);
        }
        Fill(addresses, socket);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill(ReadOnlySpan<ulong> addresses, XdpSocket? socket = null)
    {
        var count = (uint)addresses.Length;
        ref var address = ref MemoryMarshal.GetReference(addresses);
        while (count > 0)
        {
            var n = Reserve(count, out var idx);
            if (n == 0)
            {
                if (socket is null)
                    Thread.SpinWait(1);
                else
                {
                    var pollfd = new LibC.pollfd
                    {
                        fd = socket.Descriptor,
                        events = LibC.POLLIN
                    };
                    LibC.poll(&pollfd, 1, -1).ThrowIfError(); // TODO: Handle timeout better
                }
                continue;
            }

            var lastIdx = idx + n;
            for (var i = idx; i < lastIdx; ++i)
            {
                Address(i) = address;
                address = ref Unsafe.Add(ref address, 1);
            }
            Submit(n);
            count -= n;
        }
    }
}