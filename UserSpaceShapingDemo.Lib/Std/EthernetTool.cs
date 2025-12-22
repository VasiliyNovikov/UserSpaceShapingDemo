using System.Net.Sockets;
using System.Text;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static unsafe class EthernetTool
{
    private static void Command<TCommand>(string ifName, ref TCommand cmd) where TCommand : unmanaged
    {
        using var socket = new NativeSocket(NativeAddressFamily.Inet, SocketType.Dgram, ProtocolType.IP);
        var ifReq = new LibC.ifreq();
        var len = Encoding.ASCII.GetBytes(ifName, ifReq.ifr_name);
        ifReq.ifr_name[len] = 0;
        fixed (void* pCmd = &cmd)
        {
            ifReq.ifr_data = pCmd;
            socket.IOCctl(LibC.SIOCETHTOOL, ref ifReq);
        }
    }

    public static bool Get(string ifName, EthernetFeature feature)
    {
        var eval = new LibC.ethtool_value { cmd = (uint)feature };
        Command(ifName, ref eval);
        return eval.data != 0;
    }

    public static void Set(string ifName, EthernetFeature feature, bool value)
    {
        var eval = new LibC.ethtool_value { cmd = (uint)feature + 1, data = value ? 1u : 0u };
        Command(ifName, ref eval);
    }

    private static void GetChannels(string ifName, out LibC.ethtool_channels channels)
    {
        channels = new LibC.ethtool_channels { cmd = LibC.ETHTOOL_GCHANNELS };
        Command(ifName, ref channels);
    }

    public static void GetChannels(string ifName, out EthernetChannels max, out EthernetChannels current)
    {
        GetChannels(ifName, out var channels);
        max = new(channels.max_tx, channels.max_rx, channels.max_other, channels.max_combined);
        current = new(channels.tx_count, channels.rx_count, channels.other_count, channels.combined_count);
    }

    public static void SetChannels(string ifName, uint? tx = null, uint? rx = null, uint? other = null, uint? combined = null)
    {
        GetChannels(ifName, out var channels);
        if (tx is not null)
            channels.tx_count = tx.Value;
        if (rx is not null)
            channels.rx_count = rx.Value;
        if (other is not null)
            channels.other_count = other.Value;
        if (combined is not null)
            channels.combined_count = combined.Value;
        channels.cmd = LibC.ETHTOOL_SCHANNELS;
        Command(ifName, ref channels);
    }
}