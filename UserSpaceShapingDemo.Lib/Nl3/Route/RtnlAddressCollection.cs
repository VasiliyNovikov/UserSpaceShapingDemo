using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed class RtnlAddressCollection : IDisposable, IReadOnlyCollection<RtnlAddress>
{
    private readonly NlCache _cache;

    public int Count => _cache.Count;

    internal unsafe RtnlAddressCollection(RtnlSocket sock)
    {
        LibNlRoute3.rtnl_addr_alloc_cache(sock.Sock, out var c).ThrowIfError();
        _cache = new NlCache(c);
    }

    public void Dispose() => _cache.Dispose();

    public unsafe IEnumerator<RtnlAddress> GetEnumerator() => _cache.Select(obj => new RtnlAddress((LibNlRoute3.rtnl_addr*)obj.Obj, false)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}