using System;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

public class NlException(int error) : Exception($"Netlink error: {error} - {LibNl3.nl_geterror(error)}")
{
    public int ErrorCode => error;

    public static NlException FromSysError(int error) => new(LibNl3.nl_syserr2nlerr(error));

    public static NlException FromLastPInvokeError() => FromSysError(Marshal.GetLastPInvokeError());
}