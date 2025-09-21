using System;
using System.IO;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public unsafe class NativeFile(string path, NativeFileFlags flags, UnixFileMode mode)
    : FileObject(LibC.open(path, flags, mode).ThrowIfError())
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(Span<byte> buffer)
    {
        fixed (byte* ptr = buffer)
            return (int)base.Read(ptr, (nuint)buffer.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Write(ReadOnlySpan<byte> buffer)
    {
        fixed (byte* ptr = buffer)
            return (int)base.Write(ptr, (nuint)buffer.Length);
    }
}