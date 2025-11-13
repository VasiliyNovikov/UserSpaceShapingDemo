using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Dpdk;

public static class DpdkErrorExtensions
{
    private static readonly string?[] ErrorMessageCache = new string[1024];

    extension(NativeErrorNumber errorNumber)
    {
        public static NativeErrorNumber DpdkLast
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => LibDpdk.rte_errno;
        }

        public unsafe string DpdkMessage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var errorNumberInt = (int)errorNumber;
                return (uint)errorNumberInt < ErrorMessageCache.Length
                    ? ErrorMessageCache[errorNumberInt] ??= GetMessage(errorNumber)
                    : GetMessage(errorNumber);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static string GetMessage(NativeErrorNumber errorNumber) => Utf8StringMarshaller.ConvertToManaged(LibDpdk.rte_strerror(errorNumber))!;
            }
        }
    }
}