using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ICMPv6Header
{
    public readonly ICMPv6Type Type;
    public readonly byte Code;
    private readonly NetInt<ushort> _checksum;
}