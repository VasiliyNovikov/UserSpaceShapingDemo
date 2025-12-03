using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public static class RtnlVEthLink
{
    extension(RtnlLink link)
    {
        public unsafe bool IsVEth => LibNlRoute3.rtnl_link_is_veth(link.Link) != 0;
    }
}