using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib;

public sealed class FillRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref ulong Address(uint idx) => ref base.Address(idx);
}