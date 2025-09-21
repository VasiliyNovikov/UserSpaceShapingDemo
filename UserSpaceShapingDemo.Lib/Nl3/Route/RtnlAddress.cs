using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlAddress : NativeObject
{
    internal LibNlRoute3.rtnl_addr* Addr { get; }

    public int IfIndex
    {
        get => LibNlRoute3.rtnl_addr_get_ifindex(Addr);
        set => LibNlRoute3.rtnl_addr_set_ifindex(Addr, value);
    }

    public NlAddress? Address
    {
        get
        {
            var addr = LibNlRoute3.rtnl_addr_get_local(Addr);
            return addr is null ? null : new NlAddress(addr, false);
        }
        set
        {
            var error = LibNlRoute3.rtnl_addr_set_local(Addr, value is null ? null : value.Addr);
            if (error < 0)
                throw new NlException(error);
        }
    }

    public RtnlAddress()
    {
        var addr = LibNlRoute3.rtnl_addr_alloc();
        if (addr is null)
            throw NlException.FromLastNativeError();
        Addr = addr;
    }

    protected override void ReleaseUnmanagedResources()
    {
        if (Addr is not null)
            LibNlRoute3.rtnl_addr_put(Addr);
    }
}