using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public abstract class ConsumerRingBuffer : RingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Peek(uint count, out uint idx) => LibBpf.xsk_ring_cons__peek(ref Ring, count, out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(uint count) => LibBpf.xsk_ring_cons__release(ref Ring, count);
}