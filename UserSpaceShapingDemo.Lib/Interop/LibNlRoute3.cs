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

    // void rtnl_link_set_name(struct rtnl_link *link, const char *name);
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_name", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void rtnl_link_set_name(rtnl_link* link, string name);

    // int rtnl_link_add(struct nl_sock *sk, struct rtnl_link *link, int flags);
    [LibraryImport(Lib, EntryPoint = "rtnl_link_add")]
    public static partial int rtnl_link_add(LibNl3.nl_sock* sk, rtnl_link* link, int flags);

    // void rtnl_link_put(struct rtnl_link *link);
    [LibraryImport(Lib, EntryPoint = "rtnl_link_put")]
    public static partial void rtnl_link_put(rtnl_link* link);

    // struct rtnl_link *rtnl_link_veth_alloc(void);
    [LibraryImport(Lib, EntryPoint = "rtnl_link_veth_alloc", SetLastError = true)]
    public static partial rtnl_link* rtnl_link_veth_alloc();

    // struct rtnl_link* rtnl_link_veth_get_peer(struct rtnl_link *link);
    [LibraryImport(Lib, EntryPoint = "rtnl_link_veth_get_peer", SetLastError = true)]
    public static partial rtnl_link* rtnl_link_veth_get_peer(rtnl_link* link);

    [StructLayout(LayoutKind.Sequential)]
    public struct rtnl_link;
}