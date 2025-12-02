using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static class NativeString
{
    [SkipLocalsInit]
    public static unsafe string Format(byte* format, void* ap)
    {
        var bufferSize = LibC.vsnprintf(null, 0, format, ap);

        if (bufferSize < 0)
            throw new FormatException("String formatting failed.");

        if (bufferSize == 0)
            return string.Empty;

        Span<byte> buffer = stackalloc byte[bufferSize];
        var bufferPtr = (byte*)Unsafe.AsPointer(ref buffer[0]);
        return LibC.vsnprintf(bufferPtr, (UIntPtr)bufferSize, format, ap) < bufferSize
            ? Utf8StringMarshaller.ConvertToManaged(bufferPtr)!
            : throw new FormatException("String formatting failed: buffer too small.");
    }
}