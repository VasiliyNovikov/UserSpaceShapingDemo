using System.Diagnostics.CodeAnalysis;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlVethLink : RtnlLink
{
    [field: AllowNull, MaybeNull]
    public RtnlLink Peer => field ??= new RtnlLink(LibNlRoute3.rtnl_link_veth_get_peer(Link), false);

    private RtnlVethLink(LibNlRoute3.rtnl_link* link) : base(link, true) { }

    public static RtnlVethLink Allocate()
    {
        var link = LibNlRoute3.rtnl_link_veth_alloc();
        return link is null
            ? throw NlException.FromLastPInvokeError()
            : new(link);
    }
}