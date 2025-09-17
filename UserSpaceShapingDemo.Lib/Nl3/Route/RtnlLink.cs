using System;
using System.Runtime.ConstrainedExecution;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public unsafe class RtnlLink : CriticalFinalizerObject, IDisposable
{
    private readonly bool _owned;

    internal LibNlRoute3.rtnl_link* Link { get; }

    public string? Name
    {
        get => LibNlRoute3.rtnl_link_get_name(Link);
        set => LibNlRoute3.rtnl_link_set_name(Link, value);
    }

    internal RtnlLink(LibNlRoute3.rtnl_link* link, bool owned)
    {
        Link = link;
        _owned = owned;
    }

    protected virtual void ReleaseUnmanagedResources()
    {
        if (Link is not null && _owned)
            LibNlRoute3.rtnl_link_put(Link);
    }

    ~RtnlLink() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}