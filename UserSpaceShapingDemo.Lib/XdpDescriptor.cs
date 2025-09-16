using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib;

[StructLayout(LayoutKind.Sequential)]
public struct XdpDescriptor
{
    public ulong Address;
    public uint Length;
    public uint Options;
}