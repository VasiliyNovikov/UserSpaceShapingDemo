using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static class NativeTime
{
    public static long MonotonicNs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            LibC.clock_gettime(LibC.CLOCK_MONOTONIC, out var time).ThrowIfError();
            return time.tv_sec * 1_000_000_000L + time.tv_nsec;
        }
    }
}