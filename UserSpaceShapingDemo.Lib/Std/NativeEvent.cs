using System.Runtime.CompilerServices;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed class NativeEvent(bool isSet)
    : NativeEventBase(isSet ? 1u : 0u, 0)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set() => WriteOne();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Wait() => Read();
}