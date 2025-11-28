using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public static class XdpProgram
{
    public static void GetMap(int ifIndex, out FileDescriptor mapDescriptor) => LibXdp.xsk_setup_xdp_prog(ifIndex, out mapDescriptor).ThrowIfError();
}