using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlLink : NativeObject
{
    private readonly bool _owned;

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

    public bool IsVEth => LibNlRoute3.rtnl_link_is_veth(Link) != 0;

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

    public FileDescriptor NsFd
    {
        get => LibNlRoute3.rtnl_link_get_ns_fd(Link);
        set => LibNlRoute3.rtnl_link_set_ns_fd(Link, value);
    }

    public uint RxQueueCount
    {
        get => LibNlRoute3.rtnl_link_get_num_rx_queues(Link);
        set => LibNlRoute3.rtnl_link_set_num_rx_queues(Link, value);
    }

    internal RtnlLink(LibNlRoute3.rtnl_link* link, bool owned)
    {
        Link = link;
        _owned = owned;
    }

    protected override void ReleaseUnmanagedResources()
    {
        if (Link is not null && _owned)
            LibNlRoute3.rtnl_link_put(Link);
    }

    public static RtnlLink Allocate()
    {
        var link = LibNlRoute3.rtnl_link_alloc();
        return link is null
            ? throw NlException.FromLastNativeError()
            : new(link, true);
    }
}