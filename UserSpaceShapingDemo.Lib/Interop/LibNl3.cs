using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibNl3
{
    private const string Lib = "libnl-3";

    public const int NETLINK_ROUTE = 0;  // Routing/device hook

    // const char *nl_geterror(int);
    [LibraryImport(Lib, EntryPoint = "nl_geterror", StringMarshalling = StringMarshalling.Utf8)]
    public static partial byte* nl_geterror(int error);
    
    // int nl_syserr2nlerr(int);
    [LibraryImport(Lib, EntryPoint = "syserr2nlerr")]
    public static partial int nl_syserr2nlerr(NativeErrorNumber error);

    // struct nl_sock *nl_socket_alloc(void);
    [LibraryImport(Lib, EntryPoint = "nl_socket_alloc")]
    public static partial nl_sock* nl_socket_alloc();

    // void nl_socket_free(struct nl_sock *)
    [LibraryImport(Lib, EntryPoint = "nl_socket_free")]
    public static partial void nl_socket_free(nl_sock* sock);

    // int nl_connect(struct nl_sock *, int)
    [LibraryImport(Lib, EntryPoint = "nl_connect")]
    public static partial nl_api_result nl_connect(nl_sock* sock, int protocol);

    // int nl_addr_parse(const char *addrstr, int hint, struct nl_addr **result)
    [LibraryImport(Lib, EntryPoint = "nl_addr_parse", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nl_api_result nl_addr_parse(string addrstr, int hint, out nl_addr* result);

    // struct nl_addr *nl_addr_build(int family, void *buf, size_t size)
    [LibraryImport(Lib, EntryPoint = "nl_addr_build")]
    public static partial nl_addr* nl_addr_build(int family, void* buf, nuint size);
    
    // char *nl_addr2str(struct nl_addr *addr, char *buf, size_t size)
    [LibraryImport(Lib, EntryPoint = "nl_addr2str")]
    public static partial byte* nl_addr2str(nl_addr* addr, byte* buf, nuint size);

    // unsigned int nl_addr_get_prefixlen(struct nl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "nl_addr_get_prefixlen")]
    public static partial uint nl_addr_get_prefixlen(nl_addr* addr);

    // void nl_addr_set_prefixlen(struct nl_addr *addr, int prefixlen)
    [LibraryImport(Lib, EntryPoint = "nl_addr_set_prefixlen")]
    public static partial void nl_addr_set_prefixlen(nl_addr* addr, int prefixlen);

    // int nl_addr_get_family(struct nl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "nl_addr_get_family")]
    public static partial int nl_addr_get_family(nl_addr* addr);

    // void nl_addr_set_family(struct nl_addr *addr, int family)
    [LibraryImport(Lib, EntryPoint = "nl_addr_set_family")]
    public static partial void nl_addr_set_family(nl_addr* addr, int family);

    // unsigned int nl_addr_get_len(struct nl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "nl_addr_get_len")]
    public static partial uint nl_addr_get_len(nl_addr* addr);
    
    // void* nl_addr_get_binary_addr(struct nl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "nl_addr_get_binary_addr")]
    public static partial void* nl_addr_get_binary_addr(nl_addr* addr);

    // void nl_addr_put(struct nl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "nl_addr_put")]
    public static partial void nl_addr_put(nl_addr* addr);

    // void nl_cache_free(struct nl_cache *cache)
    [LibraryImport(Lib, EntryPoint = "nl_cache_free")]
    public static partial void nl_cache_free(nl_cache* cache);

    // struct nl_object *nl_cache_get_first(struct nl_cache *cache)
    [LibraryImport(Lib, EntryPoint = "nl_cache_get_first")]
    public static partial nl_object* nl_cache_get_first(nl_cache* cache);

    // struct nl_object *nl_cache_get_next(struct nl_object *obj)
    [LibraryImport(Lib, EntryPoint = "nl_cache_get_next")]
    public static partial nl_object* nl_cache_get_next(nl_object* obj);

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct nl_api_result { public readonly int error_code; }

    [StructLayout(LayoutKind.Sequential)]
    public struct nl_object;

    [StructLayout(LayoutKind.Sequential)]
    public struct nl_sock;

    [StructLayout(LayoutKind.Sequential)]
    public struct nl_addr;

    [StructLayout(LayoutKind.Sequential)]
    public struct nl_cache;
}