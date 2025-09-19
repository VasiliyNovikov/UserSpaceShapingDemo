using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

public unsafe class NlException(int error)
    : Exception($"Netlink error: {error} - {Utf8StringMarshaller.ConvertToManaged(LibNl3.nl_geterror(error))}")
{
    public static NlException FromLastPInvokeError() => new(LibNl3.nl_syserr2nlerr(Marshal.GetLastPInvokeError()));
}

internal static class NlExceptionExtensions
{
    extension(LibNl3.nl_api_result result)
    {
        public void ThrowIfError()
        {
            if (result.error_code < 0)
                throw new NlException(result.error_code);
        }
    }
}