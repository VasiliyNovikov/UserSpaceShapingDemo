using System.Diagnostics.CodeAnalysis;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlVEthLink : RtnlLink
{
    private LibNlRoute3.rtnl_link* PeerLink
    {
        get
        {
            var peerLink = LibNlRoute3.rtnl_link_veth_get_peer(Link);
            return peerLink is null
                ? throw NlException.FromLastPInvokeError()
                : peerLink;
        }
    }

    [field: AllowNull, MaybeNull]
    public RtnlLink Peer => field ??= new RtnlLink(PeerLink, false);

    internal RtnlVEthLink(LibNlRoute3.rtnl_link* link, bool owned) : base(link, owned) { }

    protected override void ReleaseUnmanagedResources()
    {
        if (Link is not null && Owned)
            LibNlRoute3.rtnl_link_put(PeerLink);
        base.ReleaseUnmanagedResources();
    }

    public static new RtnlVEthLink Allocate()
    {
        var link = LibNlRoute3.rtnl_link_veth_alloc();
        return link is null
            ? throw NlException.FromLastPInvokeError()
            : new(link, true);
    }
}