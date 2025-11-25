using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Std;

public class NativeException(NativeErrorNumber errorNumber)
    : Exception(errorNumber.Message)
{
    public NativeErrorNumber ErrorNumber => errorNumber;

    public static NativeException FromLastError() => new(NativeErrorNumber.Last);
}

public static class NativeExceptionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsError<T>(this T result) where T : unmanaged, ISignedNumber<T> => result == T.NegativeOne;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfError<T>(this T result) where T : unmanaged, ISignedNumber<T> => result.IsError() ? throw NativeException.FromLastError() : result;

    public static FileDescriptor ThrowIfError(this FileDescriptor result)
    {
        return Unsafe.BitCast<FileDescriptor, int>(result) == -1 ? throw NativeException.FromLastError() : result;
    }
}