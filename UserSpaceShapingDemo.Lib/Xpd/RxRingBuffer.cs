using System;
using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class RxRingBuffer : ConsumerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ref readonly XdpDescriptor Descriptor(uint idx) => ref base.Descriptor(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReceiveScope Receive(Span<XdpDescriptor> buffer)
    {
        var count = Peek((uint)buffer.Length, out var startIdx);
        if (count == 0)
            return default;
        for (uint i = 0; i < count; ++i)
            buffer[(int)i] = Descriptor(startIdx + i);
        return new(this, buffer, count);
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref struct ReceiveScope(RxRingBuffer ringBuffer, ReadOnlySpan<XdpDescriptor> buffer, uint count) : IDisposable
    {
        public ReadOnlySpan<XdpDescriptor> Packets
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        } = buffer[..(int)count];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!Packets.IsEmpty)
                ringBuffer.Release((uint)Packets.Length);
        }
    }
}