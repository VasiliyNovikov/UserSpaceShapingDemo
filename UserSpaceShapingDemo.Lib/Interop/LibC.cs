using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using LinuxCore;

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

    public const int MSG_DONTWAIT = 0x40;

    public const int RLIMIT_MEMLOCK = 8;
    public const long RLIM_INFINITY = -1;

    public const int SIOCETHTOOL = 0x8946;

    public const uint ETHTOOL_GRXCSUM   = 0x00000014; // Get RX hw csum enable (ethtool_value)
    public const uint ETHTOOL_SRXCSUM   = 0x00000015; // Set RX hw csum enable (ethtool_value)
    public const uint ETHTOOL_GTXCSUM   = 0x00000016; // Get TX hw csum enable (ethtool_value)
    public const uint ETHTOOL_STXCSUM   = 0x00000017; // Set TX hw csum enable (ethtool_value)
    public const uint ETHTOOL_GSG       = 0x00000018; // Get scatter-gather enable (ethtool_value)
    public const uint ETHTOOL_SSG       = 0x00000019; // Set scatter-gather enable (ethtool_value)
    public const uint ETHTOOL_GTSO      = 0x0000001e; // Get TSO enable (ethtool_value)
    public const uint ETHTOOL_STSO      = 0x0000001f; // Set TSO enable (ethtool_value)
    public const uint ETHTOOL_GUFO      = 0x00000021; // Get UFO enable (ethtool_value)
    public const uint ETHTOOL_SUFO      = 0x00000022; // Set UFO enable (ethtool_value)
    public const uint ETHTOOL_GGSO      = 0x00000023; // Get GSO enable (ethtool_value)
    public const uint ETHTOOL_SGSO      = 0x00000024; // Set GSO enable (ethtool_value)
    public const uint ETHTOOL_GGRO      = 0x0000002b; // Get GRO enable (ethtool_value)
    public const uint ETHTOOL_SGRO      = 0x0000002c; // Set GRO enable (ethtool_value)
    public const uint ETHTOOL_GCHANNELS = 0x0000003c; // Get number of channels (ethtool_channels)
    public const uint ETHTOOL_SCHANNELS = 0x0000003d; // Set number of channels (ethtool_channels)

    // int unshare (int __flags)
    [LibraryImport(Lib, EntryPoint = "unshare")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial LinuxResult unshare(int flags);

    // int setns (int __fd, int __nstype)
    [LibraryImport(Lib, EntryPoint = "setns")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial LinuxResult setns(FileDescriptor fd, int nstype);

    // int mount (const char *__special_file, const char *__dir, const char *__fstype, unsigned long int __rwflag, const void *__data)
    [LibraryImport(Lib, EntryPoint = "mount", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial LinuxResult mount(string specialFile, string dir, string? fstype, ulong rwflag, void* data);

    // int umount2 (const char *__special_file, int __flags)
    [LibraryImport(Lib, EntryPoint = "umount2", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial LinuxResult umount2(string specialFile, int flags);

    // unsigned int if_nametoindex(const char *ifname);
    [LibraryImport(Lib, EntryPoint = "if_nametoindex", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial uint if_nametoindex(string ifname);

    // ssize_t sendto(int socket, const void *message, size_t length, int flags, const struct sockaddr *dest_addr, socklen_t dest_len);
    [LibraryImport(Lib, EntryPoint = "sendto")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial LinuxResult<nint> sendto(FileDescriptor socket, void* message, nuint length, int flags, void* dest_addr, uint dest_len);

    // int setrlimit(int resource, const struct rlimit *rlim);
    [LibraryImport(Lib, EntryPoint = "setrlimit")]
    public static partial LinuxResult setrlimit(int resource, in rlimit rlim);

    [StructLayout(LayoutKind.Sequential)]
    public struct rlimit {
        public long rlim_cur;
        public long rlim_max;
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

    [StructLayout(LayoutKind.Sequential)]
    public struct ethtool_channels
    {
        public uint	cmd;
        public uint	max_rx;
        public uint	max_tx;
        public uint	max_other;
        public uint	max_combined;
        public uint	rx_count;
        public uint	tx_count;
        public uint	other_count;
        public uint	combined_count;
    }
}