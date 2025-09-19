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

    // rtnl_link flags
    public const int IFF_UP          = 0x00001;       // Interface is up.
    public const int IFF_BROADCAST   = 0x00002;       // Broadcast address valid.
    public const int IFF_DEBUG       = 0x00004;       // Turn on debugging
    public const int IFF_LOOPBACK    = 0x00008;       // Is a loopback net.
    public const int IFF_POINTOPOINT = 0x00010;       // Interface is point-to-point link.
    public const int IFF_NOTRAILERS  = 0x00020;       // Avoid use of trailers.
    public const int IFF_RUNNING     = 0x00040;       // Resources allocated.
    public const int IFF_NOARP       = 0x00080;       // No address resolution protocol.
    public const int IFF_PROMISC     = 0x00100;       // Receive all packets.
    public const int IFF_ALLMULTI    = 0x00200;       // Receive all multicast packets.
    public const int IFF_MASTER      = 0x00400;       // Master of a load balancer.
    public const int IFF_SLAVE       = 0x00800;       // Slave of a load balancer.
    public const int IFF_MULTICAST   = 0x01000;       // Supports multicast.
    public const int IFF_PORTSEL     = 0x02000;       // Can set media type.
    public const int IFF_AUTOMEDIA   = 0x04000;       // Auto media select active.
    public const int IFF_DYNAMIC     = 0x08000;       // Dialup device with changing addresses.
    public const int IFF_LOWER_UP    = 0x10000;       // Driver signals L1 up
    public const int IFF_DORMANT     = 0x20000;       // Driver signals dormant
    public const int IFF_ECHO        = 0x40000;       // Echo sent packets

    // struct rtnl_link* rtnl_link_alloc(void)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_alloc", SetLastError = true)]
    public static partial rtnl_link* rtnl_link_alloc();

    // void rtnl_link_put(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_put")]
    public static partial void rtnl_link_put(rtnl_link* link);

    // void rtnl_link_set_name(struct rtnl_link *link, const char *name)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_name", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void rtnl_link_set_name(rtnl_link* link, string? name);

    // char* rtnl_link_get_name(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_name", StringMarshalling = StringMarshalling.Utf8)]
    public static partial byte* rtnl_link_get_name(rtnl_link* link);

    // int rtnl_link_get_ifindex(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_ifindex")]
    public static partial int rtnl_link_get_ifindex(rtnl_link* link);

    // void rtnl_link_set_ifindex(struct rtnl_link *link, int ifindex)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_ifindex")]
    public static partial void rtnl_link_set_ifindex(rtnl_link* link, int ifindex);

    // unsigned int rtnl_link_get_flags(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_flags")]
    public static partial uint rtnl_link_get_flags(rtnl_link* link);

    // void rtnl_link_set_flags(struct rtnl_link *link, unsigned int flags)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_flags")]
    public static partial void rtnl_link_set_flags(rtnl_link* link, uint flags);

    // void rtnl_link_unset_flags(struct rtnl_link *link, unsigned int flags)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_unset_flags")]
    public static partial void rtnl_link_unset_flags(rtnl_link* link, uint flags);

    // void rtnl_link_set_ns_fd(struct rtnl_link *link, int fd)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_ns_fd")]
    public static partial void rtnl_link_set_ns_fd(rtnl_link* link, int fd);

    // int rtnl_link_get_ns_fd(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_ns_fd")]
    public static partial int rtnl_link_get_ns_fd(rtnl_link* link);

    // uint32_t rtnl_link_get_num_rx_queues(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_get_num_rx_queues")]
    public static partial uint rtnl_link_get_num_rx_queues(rtnl_link* link);

    // void rtnl_link_set_num_rx_queues(struct rtnl_link *link, uint32_t nqueues)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_set_num_rx_queues")]
    public static partial void rtnl_link_set_num_rx_queues(rtnl_link* link, uint nqueues);

    // struct rtnl_link *rtnl_link_veth_alloc(void)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_veth_alloc", SetLastError = true)]
    public static partial rtnl_link* rtnl_link_veth_alloc();

    // void rtnl_link_veth_release(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_veth_release")]
    public static partial void rtnl_link_veth_release(rtnl_link* link);

    // struct rtnl_link* rtnl_link_veth_get_peer(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_veth_get_peer", SetLastError = true)]
    public static partial rtnl_link* rtnl_link_veth_get_peer(rtnl_link* link);
    
    // int rtnl_link_is_veth(struct rtnl_link *link)
    [LibraryImport(Lib, EntryPoint = "rtnl_link_is_veth")]
    public static partial int rtnl_link_is_veth(rtnl_link* link);
    
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

    // struct rtnl_addr *rtnl_addr_alloc(void)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_alloc", SetLastError = true)]
    public static partial rtnl_addr* rtnl_addr_alloc();

    // void rtnl_addr_put(struct rtnl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_put")]
    public static partial void rtnl_addr_put(rtnl_addr* addr);

    // int rtnl_addr_get_ifindex(struct rtnl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_get_ifindex")]
    public static partial int rtnl_addr_get_ifindex(rtnl_addr* addr);

    // void rtnl_addr_set_ifindex(struct rtnl_addr *addr, int ifindex)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_set_ifindex")]
    public static partial void rtnl_addr_set_ifindex(rtnl_addr* addr, int ifindex);

    // struct nl_addr *rtnl_addr_get_local(struct rtnl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_get_local")]
    public static partial LibNl3.nl_addr* rtnl_addr_get_local(rtnl_addr* addr);

    // int rtnl_addr_set_local(struct rtnl_addr *addr, struct nl_addr *local)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_set_local")]
    public static partial int rtnl_addr_set_local(rtnl_addr* addr, LibNl3.nl_addr* local);

    // int rtnl_addr_add(struct nl_sock *sk, struct rtnl_addr *addr, int flags)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_add")]
    public static partial int rtnl_addr_add(LibNl3.nl_sock* sk, rtnl_addr* addr, int flags);

    // int rtnl_addr_delete(struct nl_sock *sk, struct rtnl_addr *addr, int flags)
    [LibraryImport(Lib, EntryPoint = "rtnl_addr_delete")]
    public static partial int rtnl_addr_delete(LibNl3.nl_sock* sk, rtnl_addr* addr, int flags);

    [StructLayout(LayoutKind.Sequential)]
    public struct rtnl_link;

    [StructLayout(LayoutKind.Sequential)]
    public struct rtnl_addr;
}