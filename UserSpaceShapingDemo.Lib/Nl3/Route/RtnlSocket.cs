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

    public RtnlLinkCollection GetLinks(AddressFamily family = AddressFamily.Unspecified) => new(this, family);

    public void AddLink(RtnlLink link, RtnlLinkUpdateMode mode = RtnlLinkUpdateMode.Create | RtnlLinkUpdateMode.Exclusive)
    {
        LibNlRoute3.rtnl_link_add(Sock, link.Link, (int)mode).ThrowIfError();
    }

    public void UpdateLink(RtnlLink link, RtnlLinkUpdateMode mode = RtnlLinkUpdateMode.None)
    {
        LibNlRoute3.rtnl_link_change(Sock, link.Link, link.Link, (int)mode).ThrowIfError();
    }

    public void DeleteLink(RtnlLink link) => LibNlRoute3.rtnl_link_delete(Sock, link.Link).ThrowIfError();

    public RtnlAddressCollection GetAddresses() => new(this);

    public void AddAddress(RtnlAddress addr) => LibNlRoute3.rtnl_addr_add(Sock, addr.Addr, 0).ThrowIfError();

    public void DeleteAddress(RtnlAddress addr) => LibNlRoute3.rtnl_addr_delete(Sock, addr.Addr, 0).ThrowIfError();
}