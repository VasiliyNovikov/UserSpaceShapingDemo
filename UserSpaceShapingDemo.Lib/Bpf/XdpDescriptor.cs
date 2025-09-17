using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Bpf;

[StructLayout(LayoutKind.Sequential)]
public struct XdpDescriptor
{
    public ulong Address;
    public uint Length;
    public uint Options;
}