using System;
using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class TxRingBuffer : ProducerRingBuffer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PacketRange Send(uint count)
    {
        count = Reserve(count, out var startIdx);
        return count == 0 ? default : new PacketRange(this, startIdx, count);
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct PacketRange(TxRingBuffer ringBuffer, uint index, uint length)
    {
        public uint Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => length;
        }

        public ref XdpDescriptor this[uint i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, length);
                return ref ringBuffer.Descriptor(index + i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Submit()
        {
            if (length > 0)
                ringBuffer.Submit(length);
        }
    }
}