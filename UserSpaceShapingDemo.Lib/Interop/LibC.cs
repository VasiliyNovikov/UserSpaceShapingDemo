using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Std;

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

    public const int EFD_SEMAPHORE = 0x00001; // Semaphore semantics for eventfd
    public const int EFD_NONBLOCK  = 0x00800; // Set non-blocking mode
    public const int EFD_CLOEXEC   = 0x80000; // Set close-on-exec flag

    public const short POLLIN   = 0b000001; // There is data to read
    public const short POLLPRI  = 0b000010; // There is urgent data to read
    public const short POLLOUT  = 0b000100; // Writing now not block
    public const short POLLERR  = 0b001000; // Error condition
    public const short POLLHUP  = 0b010000; // Hung up
    public const short POLLNVAL = 0b100000; // Invalid request: fd not open

    public const int MSG_DONTWAIT = 0x40;

    public const int SCHED_OTHER    = 0;
    public const int SCHED_FIFO     = 1;
    public const int SCHED_RR       = 2;
    public const int SCHED_BATCH    = 3;
    public const int SCHED_IDLE     = 5;
    public const int SCHED_DEADLINE = 6;

    public const int RLIMIT_MEMLOCK = 8;
    public const long RLIM_INFINITY = -1;
    
    public const int IFNAMSIZ = 16;
    
    public const int SIOCETHTOOL = 0x8946;

    public const uint ETHTOOL_GTXCSUM = 0x00000017;
    public const uint ETHTOOL_STXCSUM = 0x00000018;

    // int * __errno_location(void);
    [LibraryImport(Lib, EntryPoint = "__errno_location")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial NativeErrorNumber* __errno_location();

    // char *strerror(int errnum);
    [LibraryImport(Lib, EntryPoint = "strerror")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial byte* strerror(NativeErrorNumber errnum);

    // int close(int fd);
    [LibraryImport(Lib, EntryPoint = "close")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial int close(FileDescriptor fd);

    // int open(const char *pathname, int flags, mode_t mode);
    [LibraryImport(Lib, EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial FileDescriptor open(string path, NativeFileFlags flags, UnixFileMode mode);

    // ssize_t read(int fd, void* buf, size_t count);
    [LibraryImport(Lib, EntryPoint = "read")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial nint read(FileDescriptor fd, void* buf, nuint count);

    // ssize_t write(int fd, const void* buf, size_t count);
    [LibraryImport(Lib, EntryPoint = "write")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial nint write(FileDescriptor fd, void* buf, nuint count);

    // int ioctl(int fd, unsigned long operation, ...);
    [LibraryImport(Lib, EntryPoint = "ioctl")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial int ioctl(FileDescriptor fd, ulong operation, void* argp);

    // int socket(int domain, int type, int protocol);
    [LibraryImport(Lib, EntryPoint = "socket")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial FileDescriptor socket(NativeAddressFamily domain, SocketType type, ProtocolType protocol);

    // int unshare (int __flags)
    [LibraryImport(Lib, EntryPoint = "unshare")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial int unshare(int flags);

    // int setns (int __fd, int __nstype)
    [LibraryImport(Lib, EntryPoint = "setns")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial int setns(FileDescriptor fd, int nstype);

    // int mount (const char *__special_file, const char *__dir, const char *__fstype, unsigned long int __rwflag, const void *__data)
    [LibraryImport(Lib, EntryPoint = "mount", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial int mount(string specialFile, string dir, string? fstype, ulong rwflag, void* data);

    // int umount2 (const char *__special_file, int __flags)
    [LibraryImport(Lib, EntryPoint = "umount2", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial int umount2(string specialFile, int flags);

    // unsigned int if_nametoindex(const char *ifname);
    [LibraryImport(Lib, EntryPoint = "if_nametoindex", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial uint if_nametoindex(string ifname);

    // int eventfd(unsigned int initval, int flags);
    [LibraryImport(Lib, EntryPoint = "eventfd")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial FileDescriptor eventfd(uint initval, int flags);

    // int poll(struct pollfd *fds, nfds_t nfds, int timeout);
    [LibraryImport(Lib, EntryPoint = "poll")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial int poll(pollfd* fds, uint nfds, int timeout);

    // ssize_t sendto(int socket, const void *message, size_t length, int flags, const struct sockaddr *dest_addr, socklen_t dest_len);
    [LibraryImport(Lib, EntryPoint = "sendto")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial nint sendto(FileDescriptor socket, void* message, nuint length, int flags, void* dest_addr, uint dest_len);

    // int sched_setscheduler(pid_t pid, int policy, const struct sched_param *param);
    [LibraryImport(Lib, EntryPoint = "sched_setscheduler")]
    public static partial int sched_setscheduler(int pid, int policy, in sched_param param);

    // int setrlimit(int resource, const struct rlimit *rlim);
    [LibraryImport(Lib, EntryPoint = "setrlimit")]
    public static partial int setrlimit(int resource, in rlimit rlim);

    [LibraryImport(Lib, EntryPoint = "vsnprintf")]
    public static partial int vsnprintf(byte* str, nuint size, byte* format, void* ap);

    // int fstat(int fd, struct stat *statbuf);
    [LibraryImport(Lib, EntryPoint = "fstat")]
    public static partial int fstat(FileDescriptor fd, out stat statbuf);

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct pollfd
    {
        public readonly FileDescriptor fd; // File descriptor to poll
        public readonly short events; // Types of events poller cares about
        public readonly short revents; // Types of events that actually occurred
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct sched_param
    {
        public int sched_priority;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct rlimit {
        public long rlim_cur;
        public long rlim_max;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct timespec
    {
        public readonly long tv_sec;  // seconds
        public readonly long tv_nsec; // nanoseconds
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct stat
    {
        public readonly ulong st_dev;
        public readonly ulong st_ino;
        public readonly ulong st_nlink;
        public readonly uint st_mode;
        public readonly uint st_uid;
        public readonly uint st_gid;
        public readonly uint __pad0;
        public readonly ulong st_rdev;
        public readonly long st_size;
        public readonly long st_blksize;
        public readonly long st_blocks;
        public readonly timespec st_atim;
        public readonly timespec st_mtim;
        public readonly timespec st_ctim;
        public readonly long __glibc_reserved0;
        public readonly long __glibc_reserved1;
        public readonly long __glibc_reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ifreq
    {
        public InlineArray16<byte> ifr_name;
        public void* ifr_data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ethtool_value
    {
        public uint cmd;
        public uint data;
    }
}