using System.Collections.Generic;
using System.Net.Sockets;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlSocket() : NlSocket(NlProtocol.Route)
{
    private RtnlLink GetLink(int ifIndex, string? name)
    {
        LibNlRoute3.rtnl_link_get_kernel(Sock, ifIndex, name, out var link).ThrowIfError();
        return new(link, true);
    }

    public RtnlLink GetLink(int ifIndex) => GetLink(ifIndex, null);
    public RtnlLink GetLink(string name) => GetLink(0, name);

    public IEnumerable<RtnlLink> GetLinks(AddressFamily family = AddressFamily.Unspecified)
    {
        using var cache = Allocate(this, family);
        foreach (var obj in cache)
            yield return Cast(obj);

        yield break;

        static unsafe NlCache Allocate(RtnlSocket sock, AddressFamily family)
        {
            LibNlRoute3.rtnl_link_alloc_cache(sock.Sock, (int)family, out var c).ThrowIfError();
            return new NlCache(c);
        }

        static unsafe RtnlLink Cast(NlObject obj) => new((LibNlRoute3.rtnl_link*)obj.Obj, false);
    }

    public void AddLink(RtnlLink link, RntlLinkUpdateMode mode = RntlLinkUpdateMode.Create | RntlLinkUpdateMode.Exclusive)
    {
        LibNlRoute3.rtnl_link_add(Sock, link.Link, (int)mode).ThrowIfError();
    }

    public void UpdateLink(RtnlLink? oldLink, RtnlLink link, RntlLinkUpdateMode mode = RntlLinkUpdateMode.None)
    {
        LibNlRoute3.rtnl_link_change(Sock, oldLink is null ? null : oldLink.Link, link.Link, (int)mode).ThrowIfError();
    }

    public void DeleteLink(RtnlLink link) => LibNlRoute3.rtnl_link_delete(Sock, link.Link).ThrowIfError();

    public void AddAddress(RtnlAddress addr) => LibNlRoute3.rtnl_addr_add(Sock, addr.Addr, 0).ThrowIfError();

    public void DeleteAddress(RtnlAddress addr) => LibNlRoute3.rtnl_addr_delete(Sock, addr.Addr, 0).ThrowIfError();
}