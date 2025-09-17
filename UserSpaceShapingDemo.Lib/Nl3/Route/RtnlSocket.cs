using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlSocket() : NlSocket(NlProtocol.Route)
{
    public void Add(RtnlLink link, RntlLinkAddFlags flags = RntlLinkAddFlags.Create | RntlLinkAddFlags.Exclusive)
    {
        var error = LibNlRoute3.rtnl_link_add(Sock, link.Link, (int)flags);
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