using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IPv6FragmentHeader : IHasNextHeader
{
    private IPProtocol _nextHeader;
    private byte _reserved;
    private NetInt<ushort> _fragmentOffsetAndFlags;
    private NetInt<uint> _identification;

    public IPProtocol Protocol
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _nextHeader;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _nextHeader = value;
    }

    public ushort FragmentOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)((ushort)_fragmentOffsetAndFlags >> 3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _fragmentOffsetAndFlags = (NetInt<ushort>)(ushort)(((ushort)_fragmentOffsetAndFlags & 0x7) | (value << 3));
    }

    public bool MoreFragments
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => ((ushort)_fragmentOffsetAndFlags & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _fragmentOffsetAndFlags = (NetInt<ushort>)(ushort)(value ? (ushort)_fragmentOffsetAndFlags | 1 : (ushort)_fragmentOffsetAndFlags & ~1);
    }

    public uint Identification
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (uint)_identification;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _identification = (NetInt<uint>)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T NextHeader<T>() where T : unmanaged =>
        ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<IPv6FragmentHeader, byte>(ref Unsafe.AsRef(in this)), sizeof(IPv6FragmentHeader)));
}
