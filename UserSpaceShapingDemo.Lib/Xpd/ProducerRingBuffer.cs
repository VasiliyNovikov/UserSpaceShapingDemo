using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public abstract class ProducerRingBuffer : RingBuffer
{
    public bool NeedsWakeup
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LibXdp.xsk_ring_prod__needs_wakeup(Ring);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint Reserve(uint count, out uint idx) => LibXdp.xsk_ring_prod__reserve_aggressive(ref Ring, count, out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Submit(uint count) => LibXdp.xsk_ring_prod__submit(ref Ring, count);
}