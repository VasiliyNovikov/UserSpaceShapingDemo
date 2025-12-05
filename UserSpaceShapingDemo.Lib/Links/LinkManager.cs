using System;
using System.Collections.Generic;
using System.Linq;

using UserSpaceShapingDemo.Lib.Nl3.Route;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class LinkManager : IDisposable
{
    private readonly RtnlSocket _socket;

    public LinkManager(NetNs? ns = null)
    {
        using (NetNs.Enter(ns ?? NetNs.OpenCurrent()))
            _socket = new RtnlSocket();
    }

    public void Dispose() => _socket.Dispose();

    public Link[] Links
    {
        get
        {
            using var nlLinks = _socket.GetLinks();
            return [.. nlLinks.Select(l => l.IsVEth
                                               ? new VEthLink(_socket, l)
                                               : l.IsBridge
                                                   ? new BridgeLink(_socket, l)
                                                   : new Link(_socket, l))];
        }
    }
}