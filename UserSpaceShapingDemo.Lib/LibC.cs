#pragma warning disable IDE1006
using System;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib;

internal static partial class LibC
{
    private const string Lib = "libc.so.6";

    [LibraryImport(Lib, EntryPoint = "getpagesize")]
    public static partial int getpagesize();

    [LibraryImport(Lib, EntryPoint = "posix_memalign")]
    public static partial int posix_memalign(out IntPtr memptr, nuint alignment, nuint size);
}