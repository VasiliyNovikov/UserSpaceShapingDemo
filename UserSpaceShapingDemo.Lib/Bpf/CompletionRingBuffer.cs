using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Bpf;

public sealed class CompletionRingBuffer : ConsumerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref readonly ulong Address(uint idx) => ref base.Address(idx);
}