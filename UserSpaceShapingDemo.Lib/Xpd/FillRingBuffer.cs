using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref ulong Address(uint idx) => ref base.Address(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ReserveAll(UMemory umem, XdpSocket? socket = null)
    {
        var count = umem.FrameCount;
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
                        fd = socket.Fd,
                        events = LibC.POLLIN
                    };
                    if (LibC.poll(&pollfd, 1, -1) == -1) // TODO: Handle timeout better
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                }
                continue;
            }

            var lastIdx = idx + n;
            for (var i = idx; i < lastIdx; ++i)
                Address(i) = (ulong)i * umem.FrameSize;

            Submit(n);
            count -= n;
        }
    }
}