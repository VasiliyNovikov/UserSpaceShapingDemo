using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public static unsafe class RtnlBridgeLink
{
    public static RtnlLink Allocate()
    {
        var l = LibNlRoute3.rtnl_link_bridge_alloc();
        return l is null
            ? throw NlException.FromLastNativeError()
            : new RtnlLink(l, true);
    }

    extension(RtnlLink link)
    {
        public bool IsBridge => LibNlRoute3.rtnl_link_is_bridge(link.Link) != 0;

        public RtnlBridgePortState State
        {
            get
            {
                var state = LibNlRoute3.rtnl_link_bridge_get_port_state(link.Link);
                return state < 0
                    ? throw new NlException(state)
                    : (RtnlBridgePortState)state;
            }
            set => LibNlRoute3.rtnl_link_bridge_set_port_state(link.Link, (byte)value).ThrowIfError();
        }
    }
}