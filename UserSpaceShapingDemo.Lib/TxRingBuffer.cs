using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib;

public sealed class TxRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref XdpDescriptor Descriptor(uint idx) => ref base.Descriptor(idx);
}