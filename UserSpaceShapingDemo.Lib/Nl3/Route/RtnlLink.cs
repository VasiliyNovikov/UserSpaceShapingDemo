using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public unsafe class RtnlLink : CriticalFinalizerObject, IDisposable
{
    protected bool Owned { get; }

    internal LibNlRoute3.rtnl_link* Link { get; }

    public int IfIndex
    {
        get => LibNlRoute3.rtnl_link_get_ifindex(Link);
        set => LibNlRoute3.rtnl_link_set_ifindex(Link, value);
    }

    public string? Name
    {
        get
        {
            var namePtr = LibNlRoute3.rtnl_link_get_name(Link);
            return namePtr is null ? null : Utf8StringMarshaller.ConvertToManaged(namePtr);
        }
        set => LibNlRoute3.rtnl_link_set_name(Link, value);
    }

    public RtnlLinkFlags Flags => (RtnlLinkFlags)LibNlRoute3.rtnl_link_get_flags(Link);

    public bool Up
    {
        get => (Flags & RtnlLinkFlags.Up) != 0;
        set
        {
            if (value)
                LibNlRoute3.rtnl_link_set_flags(Link, (uint)RtnlLinkFlags.Up);
            else
                LibNlRoute3.rtnl_link_unset_flags(Link, (uint)RtnlLinkFlags.Up);
        }
    }

    public int NsFd
    {
        get => LibNlRoute3.rtnl_link_get_ns_fd(Link);
        set => LibNlRoute3.rtnl_link_set_ns_fd(Link, value);
    }

    internal RtnlLink(LibNlRoute3.rtnl_link* link, bool owned)
    {
        Link = link;
        Owned = owned;
    }

    internal static RtnlLink Create(LibNlRoute3.rtnl_link* link, bool owned)
    {
        return LibNlRoute3.rtnl_link_is_veth(link) == 0
            ? new RtnlLink(link, owned)
            : new RtnlVEthLink(link, owned);
    }

    protected virtual void ReleaseUnmanagedResources()
    {
        if (Link is not null && Owned)
            LibNlRoute3.rtnl_link_put(Link);
    }

    ~RtnlLink() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public static RtnlLink Allocate()
    {
        var link = LibNlRoute3.rtnl_link_alloc();
        return link is null
            ? throw NlException.FromLastPInvokeError()
            : Create(link, true);
    }
}