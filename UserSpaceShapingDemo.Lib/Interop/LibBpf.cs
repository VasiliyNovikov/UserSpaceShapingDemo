using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static partial class LibBpf
{
    private const string Lib = "libbpf";

    // int bpf_get_link_xdp_id(int ifindex, __u32 *prog_id, __u32 flags);
    [LibraryImport(Lib, EntryPoint = "bpf_get_link_xdp_id")]
    public static partial int bpf_get_link_xdp_id(int ifindex, out uint prog_id, uint flags);

    // int bpf_prog_get_fd_by_id(__u32 id);
    [LibraryImport(Lib, EntryPoint = "bpf_prog_get_fd_by_id")]
    public static partial FileDescriptor bpf_prog_get_fd_by_id(uint id);

    // int bpf_set_link_xdp_fd(int ifindex, int fd, __u32 flags);
    [LibraryImport(Lib, EntryPoint = "bpf_set_link_xdp_fd")]
    public static partial int bpf_set_link_xdp_fd(int ifindex, FileDescriptor fd, uint flags);
}