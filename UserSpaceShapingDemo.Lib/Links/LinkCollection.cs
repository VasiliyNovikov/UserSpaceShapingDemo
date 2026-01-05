using System;
using System.Collections;
using System.Collections.Generic;

using NetNsCore;

using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class LinkCollection : IEnumerable<Link>, IDisposable
{
    private readonly RtnlSocket _socket;
    private readonly NetNs _ns;

    public Link this[int index]
    {
        get
        {
            using var nlLink = _socket.GetLink(index);
            return Link.Create(_socket, _ns, nlLink);
        }
    }

    public Link this[string name]
    {
        get
        {
            using var nlLink = _socket.GetLink(name);
            return Link.Create(_socket, _ns, nlLink);
        }
    }

    public LinkCollection(NetNs? ns = null)
    {
        _ns = ns is null ? NetNs.OpenCurrent() : ns.Clone();
        using (NetNs.Enter(_ns))
            _socket = new RtnlSocket();
    }

    public void Dispose()
    {
        _socket.Dispose();
        _ns.Dispose();
    }

    public IEnumerator<Link> GetEnumerator()
    {
        using var nlLinks = _socket.GetLinks();
        foreach (var nlLink in nlLinks)
            yield return Link.Create(_socket, _ns, nlLink);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public (VEthLink Link, VEthLink Peer) CreateVEth(string name, string peerName, uint? rxQueueCount = null, uint? txQueueCount = null)
    {
        using var nlVethPair = RtnlVEthPair.Allocate();
        nlVethPair.Link.Name = name;
        nlVethPair.Peer.Name = peerName;
        if (rxQueueCount is { } rxCount)
            nlVethPair.Link.RXQueueCount = nlVethPair.Peer.RXQueueCount = rxCount;
        if (txQueueCount is { } txCount)
            nlVethPair.Link.TXQueueCount = nlVethPair.Peer.TXQueueCount = txCount;
        _socket.AddLink(nlVethPair.Link);
        return ((VEthLink)this[name], (VEthLink)this[peerName]);
    }

    public BridgeLink CreateBridge(string name)
    {
        using var nlBridge = RtnlBridgeLink.Allocate();
        nlBridge.Name = name;
        _socket.AddLink(nlBridge);
        return (BridgeLink)this[name];
    }

    public void Delete(Link link)
    {
        using var del = RtnlLink.Allocate();
        del.IfIndex = link.Index;
        _socket.DeleteLink(del);
    }
}