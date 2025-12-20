using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

public ref struct InternetChecksum
{
    private readonly ref NetInt<ushort> _checksum;
    private uint _sum32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InternetChecksum(ref NetInt<ushort> checksum)
    {
        _checksum = ref checksum;
        _checksum = default;
        _sum32 = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ReadOnlySpan<byte> data)
    {
        var items = MemoryMarshal.Cast<byte, NetInt<ushort>>(data);
        foreach (var item in items)
            _sum32 += (ushort)item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(ref T header) where T : unmanaged => Add(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Save()
    {
        _sum32 = (_sum32 & 0xFFFF) + (_sum32 >> 16);
        _sum32 += _sum32 >> 16;
        _checksum = (NetInt<ushort>)(ushort)~_sum32;
    }
}