using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using LinuxCore;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibC
{
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