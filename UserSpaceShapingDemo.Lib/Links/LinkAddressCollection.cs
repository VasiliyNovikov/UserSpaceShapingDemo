using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib.Nl3;
using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class LinkAddressCollection<TAddress> : IEnumerable<LinkAddress<TAddress>>
    where TAddress : unmanaged, IIPAddress<TAddress>
{
    private static AddressFamily Family => TAddress.Version == 4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;

    private readonly RtnlSocket _socket;
    private readonly int _linkIndex;

    internal LinkAddressCollection(RtnlSocket socket, int linkIndex)
    {
        _socket = socket;
        _linkIndex = linkIndex;
    }

    public void Add(LinkAddress<TAddress> address)
    {
        using var linkAddr = RtnlAddress.Alloc();
        using var addr = new NlAddress(address.Address.Bytes, Family);
        linkAddr.IfIndex = _linkIndex;
        linkAddr.Address = addr;
        _socket.AddAddress(linkAddr);
    }

    public void Remove(LinkAddress<TAddress> address)
    {
        using var linkAddr = RtnlAddress.Alloc();
        using var addr = new NlAddress(address.Address.Bytes, Family);
        addr.PrefixLength = address.PrefixLength;
        linkAddr.IfIndex = _linkIndex;
        linkAddr.Address = addr;
        _socket.DeleteAddress(linkAddr);
    }

    public void Clear()
    {
        foreach (var addr in this)
            Remove(addr);
    }

    public IEnumerator<LinkAddress<TAddress>> GetEnumerator()
    {
        using var rtnlAddresses = _socket.GetAddresses();
        foreach (var rtnlAddress in rtnlAddresses)
            if (rtnlAddress.IfIndex == _linkIndex && rtnlAddress.Address is { } nlAddress && nlAddress.Family == Family)
                yield return new LinkAddress<TAddress>(MemoryMarshal.Read<TAddress>(nlAddress.Bytes), nlAddress.PrefixLength);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}