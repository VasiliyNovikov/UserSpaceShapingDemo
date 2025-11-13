using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Dpdk;

public static class DpdkRuntime
{
    public static unsafe void Initialize(string[] args)
    {
        var nativeArgs = stackalloc byte*[args.Length];
        try
        {
            for (var i = 0; i < args.Length; ++i)
                nativeArgs[i] = Utf8StringMarshaller.ConvertToUnmanaged(args[i]);
            if (LibDpdk.rte_eal_init(args.Length, nativeArgs) == -1)
                throw DpdkException.FromLastError();
        }
        finally
        {
            for (var i = 0; i < args.Length; ++i)
                Utf8StringMarshaller.Free(nativeArgs[i]);
        }
    }
}