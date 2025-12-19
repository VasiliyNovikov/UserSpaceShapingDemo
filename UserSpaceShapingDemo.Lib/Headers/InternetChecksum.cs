using System;
using System.Runtime.CompilerServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

public static class InternetChecksum
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update(ref NetInt<ushort> checksum, ReadOnlySpan<NetInt<ushort>> data)
    {
        checksum = default;
        var sum32 = 0u;
        foreach (var item in data)
            sum32 += (ushort)item;
        sum32 = (sum32 & 0xFFFF) + (sum32 >> 16);
        sum32 += sum32 >> 16;
        checksum = (NetInt<ushort>)(ushort)~sum32;
    }
}