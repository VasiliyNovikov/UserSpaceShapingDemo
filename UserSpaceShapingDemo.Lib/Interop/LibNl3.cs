using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibNl3
{
    private const string Lib = "libnl-3";

    public const int NETLINK_ROUTE = 0;  // Routing/device hook

    // const char *nl_geterror(int);
    [LibraryImport(Lib, EntryPoint = "nl_geterror", StringMarshalling = StringMarshalling.Utf8)]
    public static partial string nl_geterror(int error);
    
    // int nl_syserr2nlerr(int);
    [LibraryImport(Lib, EntryPoint = "syserr2nlerr")]
    public static partial int nl_syserr2nlerr(int error);

    // struct nl_sock *nl_socket_alloc(void);
    [LibraryImport(Lib, EntryPoint = "nl_socket_alloc", SetLastError = true)]
    public static partial nl_sock* nl_socket_alloc();

    // void nl_socket_free(struct nl_sock *)
    [LibraryImport(Lib, EntryPoint = "nl_socket_free")]
    public static partial void nl_socket_free(nl_sock* sock);

    // int nl_connect(struct nl_sock *, int)
    [LibraryImport(Lib, EntryPoint = "nl_connect")]
    public static partial int nl_connect(nl_sock* sock, int protocol);

    // int nl_addr_parse(const char *addrstr, int hint, struct nl_addr **result)
    [LibraryImport(Lib, EntryPoint = "nl_addr_parse", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int nl_addr_parse(string addrstr, int hint, out nl_addr* result);

    // struct nl_addr *nl_addr_build(int family, void *buf, size_t size)
    [LibraryImport(Lib, EntryPoint = "nl_addr_build", SetLastError = true)]
    public static partial nl_addr* nl_addr_build(int family, void* buf, nuint size);
    
    // char *nl_addr2str(struct nl_addr *addr, char *buf, size_t size)
    [LibraryImport(Lib, EntryPoint = "nl_addr2str")]
    public static partial byte* nl_addr2str(nl_addr* addr, byte* buf, nuint size);
    
    // void nl_addr_put(struct nl_addr *addr)
    [LibraryImport(Lib, EntryPoint = "nl_addr_put")]
    public static partial void nl_addr_put(nl_addr* addr);

    [StructLayout(LayoutKind.Sequential)]
    public struct nl_sock;

    [StructLayout(LayoutKind.Sequential)]
    public struct nl_addr;
}