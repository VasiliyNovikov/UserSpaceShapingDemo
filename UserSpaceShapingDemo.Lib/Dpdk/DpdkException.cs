using System;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Dpdk;

public sealed class DpdkException(NativeErrorNumber errorNumber) : Exception(errorNumber.DpdkMessage)
{
    public static DpdkException FromLastError() => new(NativeErrorNumber.DpdkLast);
}