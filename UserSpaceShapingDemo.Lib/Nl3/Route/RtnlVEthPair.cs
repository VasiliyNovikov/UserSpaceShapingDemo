using System.Diagnostics.CodeAnalysis;

using LinuxCore;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlVEthPair : NativeObject
{
    private readonly LibNlRoute3.rtnl_link* _link;

    [field: AllowNull]
    public RtnlLink Link => field ??= new RtnlLink(_link, false);

    [field: AllowNull]
    public RtnlLink Peer => field ??= new RtnlLink(LibNlRoute3.rtnl_link_veth_get_peer(_link), false);

    private RtnlVEthPair(LibNlRoute3.rtnl_link* link) => _link = link;

    protected override void ReleaseUnmanagedResources() => LibNlRoute3.rtnl_link_veth_release(_link);

    public static RtnlVEthPair Allocate()
    {
        var l = LibNlRoute3.rtnl_link_veth_alloc();
        return l is null
            ? throw NlException.FromLastNativeError()
            : new RtnlVEthPair(l);
    }
}