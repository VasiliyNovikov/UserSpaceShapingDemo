using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibNl3
{
    private const string Lib = "libnl-3";

    public const int NETLINK_ROUTE = 0;  // Routing/device hook

    // const char *nl_geterror(int);
    [LibraryImport(Lib, EntryPoint = "nl_geterror", StringMarshalling = StringMarshalling.Utf8)]
    public static partial string nl_geterror(int error);

    // struct nl_sock *nl_socket_alloc(void);
    [LibraryImport(Lib, EntryPoint = "nl_socket_alloc", SetLastError = true)]
    public static partial nl_sock* nl_socket_alloc();

    // void nl_socket_free(struct nl_sock *);
    [LibraryImport(Lib, EntryPoint = "nl_socket_free")]
    public static partial void nl_socket_free(nl_sock* sock);

    // int nl_connect(struct nl_sock *, int);
    [LibraryImport(Lib, EntryPoint = "nl_connect")]
    public static partial int nl_connect(nl_sock* sock, int protocol);

    [StructLayout(LayoutKind.Sequential)]
    public struct nl_sock;
}