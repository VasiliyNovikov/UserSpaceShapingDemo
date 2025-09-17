using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public unsafe class RtnlLink : CriticalFinalizerObject, IDisposable
{
    private readonly bool _owned;

    internal LibNlRoute3.rtnl_link* Link { get; }

    public int IfIndex => LibNlRoute3.rtnl_link_get_ifindex(Link);

    public string? Name
    {
        get
        {
            var namePtr = LibNlRoute3.rtnl_link_get_name(Link);
            return namePtr is null ? null : Utf8StringMarshaller.ConvertToManaged(namePtr);
        }
        set => LibNlRoute3.rtnl_link_set_name(Link, value);
    }

    public int NsPid
    {
        get => LibNlRoute3.rtnl_link_get_ns_pid(Link);
        set => LibNlRoute3.rtnl_link_set_ns_pid(Link, value);
    }

    public int NsFd
    {
        get => LibNlRoute3.rtnl_link_get_ns_fd(Link);
        set => LibNlRoute3.rtnl_link_set_ns_fd(Link, value);
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