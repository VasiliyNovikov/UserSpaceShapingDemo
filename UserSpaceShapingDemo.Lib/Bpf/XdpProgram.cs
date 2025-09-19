using System.ComponentModel;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Bpf;

public static class XdpProgram
{
    public static void GetMap(int ifIndex, out int mapFd)
    {
        var error = LibBpf.xsk_setup_xdp_prog(ifIndex, out mapFd);
        if (error < 0)
            throw new Win32Exception(-error);
    }
}