using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public abstract unsafe class NativeEventBase(uint initialValue, int flags)
    : FileObject(LibC.eventfd(initialValue, flags).ThrowIfError())
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteOne()
    {
        var value = 1ul;
        Write(&value, sizeof(ulong));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Read()
    {
        Unsafe.SkipInit(out ulong buffer);
        Read(&buffer, sizeof(ulong));
    }
}