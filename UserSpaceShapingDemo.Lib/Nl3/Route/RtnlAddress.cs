using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public sealed unsafe class RtnlAddress : NativeObject
{
    internal LibNlRoute3.rtnl_addr* Addr { get; }

    public RtnlAddress()
    {
        var addr = LibNlRoute3.rtnl_addr_alloc();
        if (addr == null)
            throw NlException.FromLastPInvokeError();
        Addr = addr;
    }

    protected override void ReleaseUnmanagedResources()
    {
        throw new System.NotImplementedException();
    }
}