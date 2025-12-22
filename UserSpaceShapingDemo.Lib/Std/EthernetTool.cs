using System.Net.Sockets;
using System.Text;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static unsafe class EthernetTool
{
    private static uint GetFeature(string ifName, uint cmd)
    {
        using var socket = new NativeSocket(NativeAddressFamily.Inet, SocketType.Dgram, ProtocolType.IP);
        var ifr = new LibC.ifreq();
        var len = Encoding.ASCII.GetBytes(ifName, ifr.ifr_name);
        ifr.ifr_name[len] = 0;
        var eval = new LibC.ethtool_value { cmd = cmd };
        ifr.ifr_data = &eval;
        socket.IOCctl(LibC.SIOCETHTOOL, ref ifr);
        return eval.data;
    }

    private static void SetFeature(string ifName, uint cmd, uint value)
    {
        using var socket = new NativeSocket(NativeAddressFamily.Inet, SocketType.Dgram, ProtocolType.IP);
        var ifr = new LibC.ifreq();
        var len = Encoding.ASCII.GetBytes(ifName, ifr.ifr_name);
        ifr.ifr_name[len] = 0;
        var eval = new LibC.ethtool_value { cmd = cmd, data = value };
        ifr.ifr_data = &eval;
        socket.IOCctl(LibC.SIOCETHTOOL, ref ifr);
    }
}