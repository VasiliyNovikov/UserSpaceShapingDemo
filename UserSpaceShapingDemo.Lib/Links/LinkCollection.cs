using System;
using System.Collections;
using System.Collections.Generic;

using UserSpaceShapingDemo.Lib.Nl3.Route;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class LinkCollection : IEnumerable<Link>, IDisposable
{
    private readonly RtnlSocket _socket;

    public Link this[int index]
    {
        get
        {
            using var nlLink = _socket.GetLink(index);
            return Link.Create(_socket, nlLink);
        }
    }

    public Link this[string name]
    {
        get
        {
            using var nlLink = _socket.GetLink(name);
            return Link.Create(_socket, nlLink);
        }
    }

    public LinkCollection(NetNs? ns = null)
    {
        using (NetNs.Enter(ns ?? NetNs.OpenCurrent()))
            _socket = new RtnlSocket();
    }

    public void Dispose() => _socket.Dispose();

    public IEnumerator<Link> GetEnumerator()
    {
        using var nlLinks = _socket.GetLinks();
        foreach (var nlLink in nlLinks)
            yield return Link.Create(_socket, nlLink);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public (VEthLink Link, VEthLink Peer) CreateVEth(string name, string peerName, uint? rxQueueCount = null)
    {
        using var nlVethPair = RtnlVEthPair.Allocate();
        nlVethPair.Link.Name = name;
        nlVethPair.Peer.Name = peerName;
        if (rxQueueCount is { } count)
            nlVethPair.Link.RxQueueCount = nlVethPair.Peer.RxQueueCount = count;
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