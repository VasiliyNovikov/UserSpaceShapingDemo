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
}