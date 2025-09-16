using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib;

public abstract class ProducerRingBuffer : RingBuffer
{
    public bool NeedsWakeup
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LibBpf.xsk_ring_prod__needs_wakeup(Ring);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Reserve(uint count, out uint idx) => LibBpf.xsk_ring_prod__reserve(ref Ring, count, out idx) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Submit(uint count) => LibBpf.xsk_ring_prod__submit(ref Ring, count);
}