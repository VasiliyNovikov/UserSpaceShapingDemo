using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using LinuxCore;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibC
{
    public const int MSG_DONTWAIT = 0x40;

    // ssize_t sendto(int socket, const void *message, size_t length, int flags, const struct sockaddr *dest_addr, socklen_t dest_len);
    [LibraryImport(LinuxLibraries.LibC, EntryPoint = "sendto")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial LinuxResult<nint> sendto(FileDescriptor socket, void* message, nuint length, int flags, void* dest_addr, uint dest_len);
}