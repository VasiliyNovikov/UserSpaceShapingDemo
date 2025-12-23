using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Receive(Span<XdpDescriptor> packets)
    {
        var count = Peek((uint)packets.Length, out var startIdx);
        ref var packet = ref MemoryMarshal.GetReference(packets);
        for (var i = 0u; i < count; ++i)
        {
            packet = Descriptor(startIdx++);
            packet = ref Unsafe.Add(ref packet, 1);
        }
        Release(count);
        return count;
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