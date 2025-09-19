using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibC
{
    private const string Lib = "libc";

    public const int CLONE_NEWNET = 0x40000000;

    public const ulong MS_BIND = 4096;

    public const int MNT_FORCE = 1; // Force unmounting
    public const int MNT_DETACH = 2; // Just detach from the tree
    public const int MNT_EXPIRE = 4; // Mark for expiry
    public const int UMOUNT_NOFOLLOW = 8; // Don't follow symlink on umount

    public const short POLLIN   = 0b000001; // There is data to read
    public const short POLLPRI  = 0b000010; // There is urgent data to read
    public const short POLLOUT  = 0b000100; // Writing now not block
    public const short POLLERR  = 0b001000; // Error condition
    public const short POLLHUP  = 0b010000; // Hung up
    public const short POLLNVAL = 0b100000; // Invalid request: fd not open

    // int unshare (int __flags)
    [LibraryImport(Lib, EntryPoint = "unshare", SetLastError = true)]
    public static partial int unshare(int flags);
    
    // int setns (int __fd, int __nstype)
    [LibraryImport(Lib, EntryPoint = "setns", SetLastError = true)]
    public static partial int setns(FileDescriptor fd, int nstype);

    // int mount (const char *__special_file, const char *__dir, const char *__fstype, unsigned long int __rwflag, const void *__data)
    [LibraryImport(Lib, EntryPoint = "mount", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial int mount(string specialFile, string dir, string? fstype, ulong rwflag, void* data);

    // int umount2 (const char *__special_file, int __flags)
    [LibraryImport(Lib, EntryPoint = "umount2", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial int umount2(string specialFile, int flags);

    // unsigned int if_nametoindex(const char *ifname);
    [LibraryImport(Lib, EntryPoint = "if_nametoindex", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial uint if_nametoindex(string ifname);
    
    // int poll(struct pollfd *fds, nfds_t nfds, int timeout);
    [LibraryImport(Lib, EntryPoint = "poll", SetLastError = true)]
    public static partial int poll(pollfd* fds, uint nfds, int timeout);

    [StructLayout(LayoutKind.Sequential)]
    public struct pollfd
    {
        public FileDescriptor fd; // File descriptor to poll
        public short events; // Types of events poller cares about
        public short revents; // Types of events that actually occurred
    };
}