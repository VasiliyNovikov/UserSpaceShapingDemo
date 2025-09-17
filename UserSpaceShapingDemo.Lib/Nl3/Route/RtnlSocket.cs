using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlSocket() : NlSocket(NlProtocol.Route)
{
    private RtnlLink GetLink(int ifIndex, string? name)
    {
        var error = LibNlRoute3.rtnl_link_get_kernel(Sock, ifIndex, name, out var link);
        return error < 0
            ? throw new NlException(error)
            : link == null
                ? throw new InvalidOperationException("Link is null despite no error from rtnl_link_get_kernel")
                : new RtnlLink(link, true);
    }

    public RtnlLink GetLink(int ifIndex) => GetLink(ifIndex, null);
    public RtnlLink GetLink(string name) => GetLink(0, name);

    public void Add(RtnlLink link, RntlLinkUpdateFlags flags = RntlLinkUpdateFlags.Create | RntlLinkUpdateFlags.Exclusive)
    {
        var error = LibNlRoute3.rtnl_link_add(Sock, link.Link, (int)flags);
        if (error < 0)
            throw new NlException(error);
    }

    public void Update(RtnlLink link, RntlLinkUpdateFlags flags = RntlLinkUpdateFlags.None)
    {
        var error = LibNlRoute3.rtnl_link_change(Sock, link.Link, link.Link, (int)flags);
        if (error < 0)
            throw new NlException(error);
    }

    public void Delete(RtnlLink link)
    {
        var error = LibNlRoute3.rtnl_link_delete(Sock, link.Link);
        if (error < 0)
            throw new NlException(error);
    }
}