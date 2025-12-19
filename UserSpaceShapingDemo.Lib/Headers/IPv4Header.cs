using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IPv4Header : IIPHeader<IPv4Address>
{
    private byte _versionAndHeaderLength;
    private byte _typeOfService;
    private NetInt<ushort> _totalLength;
    private NetInt<ushort> _id;
    private NetInt<ushort> _fragmentOffset;
    private byte _ttl;
    private IPProtocol _protocol;
    private NetInt<ushort> _checksum;
    private IPv4Address _sourceAddress;
    private IPv4Address _destinationAddress;

    public byte Version
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)((_versionAndHeaderLength & 0xF0) >> 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _versionAndHeaderLength = (byte)((_versionAndHeaderLength & 0x0F) | ((value << 4) & 0xF0));
    }

    public byte TrafficClass
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _typeOfService;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _typeOfService = value;
    }

    public byte HeaderLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)((_versionAndHeaderLength & 0x0F) << 2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _versionAndHeaderLength = (byte)((_versionAndHeaderLength & 0xF0) | ((value >> 2) & 0x0F));
    }

    public ushort PayloadLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)((ushort)_totalLength - HeaderLength);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _totalLength = (NetInt<ushort>)(value + HeaderLength);
    }

    public ushort TotalLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_totalLength;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _totalLength = (NetInt<ushort>)value;
    }

    public ushort Identification
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _id = (NetInt<ushort>)value;
    }

    public ushort FragmentOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_fragmentOffset;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _fragmentOffset = (NetInt<ushort>)value;
    }

    public IPProtocol Protocol
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _protocol;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _protocol = value;
    }

    public byte Ttl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _ttl;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _ttl = value;
    }

    public IPv4Address SourceAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _sourceAddress;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _sourceAddress = value;
    }

    public IPv4Address DestinationAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _destinationAddress;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _destinationAddress = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T NextHeader<T>() where T : unmanaged => ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<IPv4Header, byte>(ref Unsafe.AsRef(ref this)), HeaderLength));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateChecksum()
    {
        _checksum = default;
        var buffer = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<IPv4Header, NetInt<ushort>>(ref this), HeaderLength / 2);
        var sum32 = 0u;
        foreach (var item in buffer)
            sum32 += (ushort)item;
        sum32 = (sum32 & 0xFFFF) + (sum32 >> 16);
        sum32 += sum32 >> 16;
        _checksum = (NetInt<ushort>)(ushort)~sum32;
    }
}