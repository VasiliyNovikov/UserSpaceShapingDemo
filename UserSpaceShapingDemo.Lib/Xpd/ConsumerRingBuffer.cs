using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public abstract class ConsumerRingBuffer : RingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint Peek(out uint idx) => LibXdp.xsk_ring_cons__peek_all(ref Ring, out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint Peek(uint count, out uint idx) => LibXdp.xsk_ring_cons__peek(ref Ring, count, out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Release(uint count) => LibXdp.xsk_ring_cons__release(ref Ring, count);
}