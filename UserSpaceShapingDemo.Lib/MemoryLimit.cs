using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib;

public static class MemoryLockLimit
{
    public static void SetInfinity() => LibC.setrlimit(LibC.RLIMIT_MEMLOCK, new LibC.rlimit { rlim_cur = LibC.RLIM_INFINITY, rlim_max = LibC.RLIM_INFINITY }).ThrowIfError();
}