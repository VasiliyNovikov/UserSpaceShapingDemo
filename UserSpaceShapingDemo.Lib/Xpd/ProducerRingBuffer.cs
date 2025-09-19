using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public abstract class ProducerRingBuffer : RingBuffer
{
    public bool NeedsWakeup
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LibBpf.xsk_ring_prod__needs_wakeup(Ring);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Reserve(uint count, out uint idx) => LibBpf.xsk_ring_prod__reserve(ref Ring, count, out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Submit(uint count) => LibBpf.xsk_ring_prod__submit(ref Ring, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReserveAndSubmit(uint count, uint totalFrameCount, uint frameSize)
    {
        while (count > 0)
        {
            var n = Reserve(count, out var idx);
            if (n == 0)
            {
                Thread.SpinWait(1); // TODO: Maybe poll the socket?
                continue;
            }

            var lastIdx = idx + n;
            for (var i = idx; i < lastIdx; ++i)
                Address(i) = ((ulong)(i % totalFrameCount)) * frameSize;

            Submit(n);
            count -= n;
        }
    }
}