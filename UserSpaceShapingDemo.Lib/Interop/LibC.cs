using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using LinuxCore;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibC
{
    public const int MSG_DONTWAIT = 0x40;

    public const int RLIMIT_MEMLOCK = 8;
    public const long RLIM_INFINITY = -1;

    // unsigned int if_nametoindex(const char *ifname);
    [LibraryImport(LinuxLibraries.LibC, EntryPoint = "if_nametoindex", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial uint if_nametoindex(string ifname);

    // ssize_t sendto(int socket, const void *message, size_t length, int flags, const struct sockaddr *dest_addr, socklen_t dest_len);
    [LibraryImport(LinuxLibraries.LibC, EntryPoint = "sendto")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial LinuxResult<nint> sendto(FileDescriptor socket, void* message, nuint length, int flags, void* dest_addr, uint dest_len);

    // int setrlimit(int resource, const struct rlimit *rlim);
    [LibraryImport(LinuxLibraries.LibC, EntryPoint = "setrlimit")]
    public static partial LinuxResult setrlimit(int resource, in rlimit rlim);

    [StructLayout(LayoutKind.Sequential)]
    public struct rlimit
    {
        public long rlim_cur;
        public long rlim_max;
    }
}