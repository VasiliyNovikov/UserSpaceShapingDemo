using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

[StructLayout(LayoutKind.Sequential)]
public struct XdpDescriptor
{
    public readonly ulong Address;
    public uint Length;
    public uint Options;
}