using System;
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
    public static int ThrowIfError(this int result) => result < 0 ? throw NativeException.FromLastError() : result;

    public static FileDescriptor ThrowIfError(this FileDescriptor result)
    {
        return Unsafe.As<FileDescriptor, int>(ref result) < 0 ? throw NativeException.FromLastError() : result;
    }
}