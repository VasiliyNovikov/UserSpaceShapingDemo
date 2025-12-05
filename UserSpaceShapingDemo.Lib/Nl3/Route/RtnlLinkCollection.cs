using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed class RtnlLinkCollection : IDisposable, IReadOnlyCollection<RtnlLink>
{
    private readonly NlCache _cache;

    public int Count => _cache.Count;

    internal unsafe RtnlLinkCollection(RtnlSocket sock, NativeAddressFamily family)
    {
        LibNlRoute3.rtnl_link_alloc_cache(sock.Sock, (int)family, out var c).ThrowIfError();
        _cache = new NlCache(c);
    }

    public void Dispose() => _cache.Dispose();

    public unsafe IEnumerator<RtnlLink> GetEnumerator() => _cache.Select(obj => new RtnlLink((LibNlRoute3.rtnl_link*)obj.Obj, false)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}