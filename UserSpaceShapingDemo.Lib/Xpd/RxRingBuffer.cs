using System;
using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class RxRingBuffer : ConsumerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref readonly XdpDescriptor Descriptor(uint idx) => ref base.Descriptor(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Receive(Span<XdpDescriptor> buffer)
    {
        var count = Peek((uint)buffer.Length, out var startIdx);
        if (count == 0)
            return 0;
        for (var i = 0; i < count; i++)
            buffer[i] = Descriptor(startIdx + (uint)i);
        Release(count);
        return count;
    }
}