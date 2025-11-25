using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UDPHeader
{
    private NetInt<ushort> _sourcePort;
    private NetInt<ushort> _destinationPort;
    private NetInt<ushort> _size;
    public NetInt<ushort> Checksum;

    public ushort SourcePort
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_sourcePort;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _sourcePort = (NetInt<ushort>)value;
    }

    public ushort DestinationPort
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_destinationPort;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _destinationPort = (NetInt<ushort>)value;
    }

    public ushort Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_size;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _size = (NetInt<ushort>)value;
    }

    public unsafe Span<byte> Payload
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.CreateSpan(ref Unsafe.As<UDPHeader, byte>(ref this), Size)[sizeof(UDPHeader)..];
    }
}