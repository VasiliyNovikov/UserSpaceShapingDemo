using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IPv4Header
{
    private byte _versionAndHeaderLength;
    public byte TypeOfService;
    private NetInt<ushort> _totalLength;
    public NetInt<ushort> Id;
    public NetInt<ushort> FragmentOffset;
    public byte Ttl;
    public IPProtocol Protocol;
    private NetInt<ushort> Checksum;
    public IPv4Address SourceAddress;
    public IPv4Address DestinationAddress;

    public byte Version
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)((_versionAndHeaderLength & 0xF0) >> 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _versionAndHeaderLength = (byte)((_versionAndHeaderLength & 0x0F) | ((value << 4) & 0xF0));
    }

    public byte HeaderLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)((_versionAndHeaderLength & 0x0F) << 2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _versionAndHeaderLength = (byte)((_versionAndHeaderLength & 0xF0) | ((value >> 2) & 0x0F));
    }

    public ushort TotalLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_totalLength;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _totalLength = (NetInt<ushort>)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Layer3Header<T>() where T : unmanaged => ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<IPv4Header, byte>(ref Unsafe.AsRef(ref this)), HeaderLength));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateChecksum()
    {
        Checksum = default;
        var buffer = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<IPv4Header, NetInt<ushort>>(ref this), HeaderLength / 2);
        var sum32 = 0u;
        foreach (var item in buffer)
            sum32 += (ushort)item;
        sum32 = (sum32 & 0xFFFF) + (sum32 >> 16);
        sum32 += sum32 >> 16;
        Checksum = (NetInt<ushort>)(ushort)~sum32;
    }
}