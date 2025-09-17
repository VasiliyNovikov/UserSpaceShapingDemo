using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibNlRoute3
{
    private const string Lib = "libnl-route-3";
    
    // Modifiers to NEW request
    public const int NLM_F_REPLACE = 0x100; // Override existing
    public const int NLM_F_EXCL    = 0x200; // Do not touch, if it exists
    public const int NLM_F_CREATE  = 0x400; // Create, if it does not exist
    public const int NLM_F_APPEND  = 0x800; // Add to end of list

    // void rtnl_link_set_name(struct rtnl_link *link, const char *name)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_name", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void rtnl_link_set_name(rtnl_link* link, string? name);

    // char* rtnl_link_get_name(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_name", StringMarshalling = StringMarshalling.Utf8)]
    public static partial byte* rtnl_link_get_name(rtnl_link* link);

    // int rtnl_link_get_ifindex(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_ifindex")]
    public static partial int rtnl_link_get_ifindex(rtnl_link* link);

    // void rtnl_link_set_ns_fd(struct rtnl_link *link, int fd)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_ns_fd")]
    public static partial void rtnl_link_set_ns_fd(rtnl_link* link, int fd);

    // int rtnl_link_get_ns_fd(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_ns_fd")]
    public static partial int rtnl_link_get_ns_fd(rtnl_link* link);

    // void rtnl_link_set_ns_pid(struct rtnl_link *link, pid_t pid)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_ns_pid")]
    public static partial void rtnl_link_set_ns_pid(rtnl_link* link, int pid);

    // int rtnl_link_get_ns_pid(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_ns_pid")]
    public static partial int rtnl_link_get_ns_pid(rtnl_link* link);

    // void rtnl_link_put(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_put")]
    public static partial void rtnl_link_put(rtnl_link* link);

    // struct rtnl_link *rtnl_link_veth_alloc(void)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_veth_alloc", SetLastError = true)]
    public static partial rtnl_link* rtnl_link_veth_alloc();

    // struct rtnl_link* rtnl_link_veth_get_peer(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_veth_get_peer", SetLastError = true)]
    public static partial rtnl_link* rtnl_link_veth_get_peer(rtnl_link* link);
    
    // int rtnl_link_add(struct nl_sock *sk, struct rtnl_link *link, int flags)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_add")]
    public static partial int rtnl_link_add(LibNl3.nl_sock* sk, rtnl_link* link, int flags);
    
    // int rtnl_link_change(struct nl_sock *sk, struct rtnl_link *orig, struct rtnl_link *changes, int flags)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_change")]
    public static partial int rtnl_link_change(LibNl3.nl_sock* sk, rtnl_link* orig, rtnl_link* changes, int flags);

    // int rtnl_link_delete(struct nl_sock *sk, const struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_delete")]
    public static partial int rtnl_link_delete(LibNl3.nl_sock* sk, rtnl_link* link);

    // int rtnl_link_get_kernel(struct nl_sock *sk, int ifindex, const char *name, struct rtnl_link **result)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_kernel", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int rtnl_link_get_kernel(LibNl3.nl_sock* sk, int ifindex, string? name, out rtnl_link* result);

    [StructLayout(LayoutKind.Sequential)]
    public struct rtnl_link;
}