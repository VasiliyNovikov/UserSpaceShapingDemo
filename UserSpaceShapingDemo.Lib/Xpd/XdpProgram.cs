using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public static class XdpProgram
{
    public static void GetMap(int ifIndex, out FileDescriptor mapFd) => LibBpf.xsk_setup_xdp_prog(ifIndex, out mapFd).ThrowIfError();
}