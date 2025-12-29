using LinuxCore;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public static class XdpProgram
{
    public static void GetMap(int ifIndex, out FileDescriptor mapDescriptor) => LibXdp.xsk_setup_xdp_prog(ifIndex, out mapDescriptor).ThrowIfError();
}