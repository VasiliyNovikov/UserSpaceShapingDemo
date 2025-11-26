using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public static class XdpProgram
{
    public static void GetMap(int ifIndex, XdpSocketMode mode, out FileDescriptor mapDescriptor)
    {
        // 1. Load the default program using the helper (attaches with flags=0, likely Generic)
        LibXdp.xsk_setup_xdp_prog(ifIndex, out mapDescriptor).ThrowIfError();

        // If we just wanted default/generic, we are done.
        if (mode is XdpSocketMode.Default or XdpSocketMode.Generic)
            return;

        // 2. Identify the program that was just loaded
        LibBpf.bpf_get_link_xdp_id(ifIndex, out var progId, 0).ThrowIfError();

        // 3. Get a file descriptor for that program so we can keep it alive while detaching
        var progFd = LibBpf.bpf_prog_get_fd_by_id(progId).ThrowIfError();

        try
        {
            // 4. Detach the Generic instance (fd = -1)
            // We use the same flags=0 because that's how it was attached
            LibBpf.bpf_set_link_xdp_fd(ifIndex, Unsafe.BitCast<int, FileDescriptor>(-1), 0).ThrowIfError();

            // 5. Re-attach the SAME program in the requested mode (e.g., Driver)
            // This ensures the socket (ZeroCopy) and Program (Driver) match.
            LibBpf.bpf_set_link_xdp_fd(ifIndex, progFd, (uint)mode).ThrowIfError();
        }
        finally
        {
            // Close our temporary reference to the program; it stays loaded because it's attached now
            LibC.close(progFd).ThrowIfError();
        }
    }
}