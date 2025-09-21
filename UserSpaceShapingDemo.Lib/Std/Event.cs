using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed unsafe class Event(bool isSet)
    : FileObject(LibC.eventfd(isSet ? 1u : 0u, 0).ThrowIfError())
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set()
    {
        ulong value = 1;
        Write(&value, sizeof(ulong));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Wait()
    {
        ulong value;
        Read(&value, sizeof(ulong));
    }
}