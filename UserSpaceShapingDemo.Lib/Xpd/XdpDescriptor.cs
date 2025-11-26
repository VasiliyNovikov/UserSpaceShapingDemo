using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Xpd;

[StructLayout(LayoutKind.Sequential)]
public struct XdpDescriptor
{
    public ulong Address;
    public uint Length;
    public readonly uint Options;
}