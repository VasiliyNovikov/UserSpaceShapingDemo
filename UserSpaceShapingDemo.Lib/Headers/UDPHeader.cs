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
    private NetInt<ushort> _checksum;

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

    public ushort Checksum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)_checksum;
    }

    public unsafe Span<byte> Payload
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.CreateSpan(ref Unsafe.As<UDPHeader, byte>(ref this), Size)[sizeof(UDPHeader)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateChecksum() => _checksum = default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateChecksum(ref IPv6Header ipv6Header)
    {
        var checksum = new InternetChecksum(ref _checksum);
        var ipv6PseudoHeader = new IPv6PseudoHeader(ref ipv6Header);
        checksum.Add(ref ipv6PseudoHeader);
        checksum.Add(ref this);
        checksum.Add(Payload);
        checksum.Save();
    }
}