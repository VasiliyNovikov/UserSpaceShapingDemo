using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class RxRingBuffer : ConsumerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref readonly XdpDescriptor Descriptor(uint idx) => ref base.Descriptor(idx);
}