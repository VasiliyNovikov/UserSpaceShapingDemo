using System;
using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class RxRingBuffer : ConsumerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PacketRange Receive(uint count)
    {
        count = Peek(count, out var startIdx);
        return count == 0 ? default : new PacketRange(this, startIdx, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PacketRange Receive()
    {
        var count = Peek(out var startIdx);
        return count == 0 ? default : new PacketRange(this, startIdx, count);
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct PacketRange(RxRingBuffer ringBuffer, uint index, uint length)
    {
        public uint Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => length;
        }

        public ref readonly XdpDescriptor this[uint i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, length);
                return ref ringBuffer.Descriptor(index + i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            if (length > 0)
                ringBuffer.Release(length);
        }
    }
}