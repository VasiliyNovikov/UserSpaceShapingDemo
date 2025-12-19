using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IPv6Header : IIPHeader<IPv6Address>
{
    private NetInt<uint> _versionTrafficClassFlowLabel;
    private NetInt<ushort> _payloadLength;
    private IPProtocol _nextHeader;
    private byte _hopLimit;
    private IPv6Address _sourceAddress;
    private IPv6Address _destinationAddress;

    public byte Version
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)(((uint)_versionTrafficClassFlowLabel & 0xF0000000u) >> 28);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _versionTrafficClassFlowLabel = (NetInt<uint>)(((uint)_versionTrafficClassFlowLabel & 0x0FFFFFFFu) | ((value << 28) & 0xF0000000u));
    }

    public byte TrafficClass
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)(((uint)_versionTrafficClassFlowLabel & 0x0FF00000u) >> 20);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _versionTrafficClassFlowLabel = (NetInt<uint>)(((uint)_versionTrafficClassFlowLabel & 0xF00FFFFFu) | (value << 20 & 0x0FF00000u));
    }

    public uint FlowLabel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (uint)_versionTrafficClassFlowLabel & 0x000FFFFFu;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _versionTrafficClassFlowLabel = (NetInt<uint>)(((uint)_versionTrafficClassFlowLabel & 0xFFF00000u) | (value & 0x000FFFFFu));
    }

    public unsafe byte HeaderLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)sizeof(IPv6Header);
        set => throw new InvalidOperationException("IPv6 Header Length is fixed and cannot be set.");
    }

    public ushort PayloadLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_payloadLength;
        set => _payloadLength = (NetInt<ushort>)value;
    }

    public ushort TotalLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)((ushort)_payloadLength + HeaderLength);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _payloadLength = (NetInt<ushort>)(value - HeaderLength);
    }

    public IPProtocol Protocol
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _nextHeader;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _nextHeader = value;
    }

    public byte Ttl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _hopLimit;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _hopLimit = value;
    }

    public IPv6Address SourceAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _sourceAddress;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _sourceAddress = value;
    }

    public IPv6Address DestinationAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _destinationAddress;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _destinationAddress = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T NextHeader<T>() where T : unmanaged => ref Unsafe.As<IPv6Header, T>(ref Unsafe.Add(ref Unsafe.AsRef(ref this), 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateChecksum()
    {
    }
}