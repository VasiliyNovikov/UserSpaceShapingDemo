using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib;

public sealed class ConsumerRingBuffer : RingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ulong CompletionAddress(uint idx) => ref Address(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Peek(uint count, out uint idx) => LibBpf.xsk_ring_cons__peek(ref Ring, count, out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(uint count) => LibBpf.xsk_ring_cons__release(ref Ring, count);
}