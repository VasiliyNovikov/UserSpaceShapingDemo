using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed unsafe class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref ulong Address(uint idx) => ref base.Address(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitFill(UMemory umem, XdpSocket? socket = null)
    {
        var count = umem.FrameCount;
        while (count > 0)
        {
            var n = ReserveWait(count, out var idx, socket);
            var lastIdx = idx + n;
            for (var i = idx; i < lastIdx; ++i)
                Address(i) = (ulong)(i % umem.FrameCount) * umem.FrameSize;
            Submit(n);
            count -= n;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill(ReadOnlySpan<ulong> addresses, XdpSocket? socket = null)
    {
        var count = (uint)addresses.Length;
        ref var address = ref MemoryMarshal.GetReference(addresses);
        while (count > 0)
        {
            var n = ReserveWait(count, out var idx, socket);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ReserveWait(uint count, out uint idx, XdpSocket? socket = null)
    {
        while (true)
        {
            var n = Reserve(count, out idx);
            if (n != 0)
                return n;
            if (socket is null)
                Thread.SpinWait(1);
            else
            {
                var pollfd = new LibC.pollfd
                {
                    fd = socket.Fd,
                    events = LibC.POLLIN
                };
                if (LibC.poll(&pollfd, 1, -1) == -1) // TODO: Handle timeout better
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }
    }
}