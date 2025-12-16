using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed class NativeSemaphore(uint initialValue = 0)
    : NativeEventBase(initialValue, LibC.EFD_SEMAPHORE | LibC.EFD_NONBLOCK)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment() => WriteOne();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Decrement() => Read();
}