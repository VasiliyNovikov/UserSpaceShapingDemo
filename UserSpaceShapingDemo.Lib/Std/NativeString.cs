using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static class NativeString
{
    public static unsafe string Format(byte* format, void* ap, int initialBufferSize = 256)
    {
        var bufferSize = initialBufferSize;
        string? result;
        while (!TryFormat(format, ap, bufferSize, out result))
            bufferSize *= 2;
        return result;
    }

    [SkipLocalsInit]
    private static unsafe bool TryFormat(byte* format, void* ap, int bufferSize, [MaybeNullWhen(false)] out string result)
    {
        Span<byte> buffer = stackalloc byte[bufferSize];
        var bufferPtr = (byte*)Unsafe.AsPointer(ref buffer[0]);
        var written = LibC.vsnprintf(bufferPtr, (UIntPtr)bufferSize, format, ap);
        if (written >= bufferSize)
        {
            result = null;
            return false;
        }

        if (written < 0)
            throw new InvalidOperationException("String formatting failed.");

        result = Utf8StringMarshaller.ConvertToManaged(bufferPtr)!;
        return true;
    }
}