using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static class NativeString
{
    [SkipLocalsInit]
    public static unsafe string Format(byte* format, void* ap, int bufferSize = 256)
    {
        Span<byte> buffer = stackalloc byte[bufferSize];
        var bufferPtr = (byte*)Unsafe.AsPointer(ref buffer[0]);
        var written = LibC.vsnprintf(bufferPtr, (UIntPtr)(bufferSize - 1), format, ap);
        if (written == bufferSize - 1)
        {
            buffer[written - 3] = (byte)'.';
            buffer[written - 2] = (byte)'.';
            buffer[written - 1] = (byte)'.';
            buffer[written] = 0;
        }
        return Utf8StringMarshaller.ConvertToManaged(bufferPtr)!;
    }
}