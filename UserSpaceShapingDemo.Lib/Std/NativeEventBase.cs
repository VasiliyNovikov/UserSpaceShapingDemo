using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public abstract unsafe class NativeEventBase(uint initialValue, int flags)
    : FileObject(LibC.eventfd(initialValue, flags).ThrowIfError())
{
    private static readonly long* Value = (long*)NativeMemory.Alloc(sizeof(ulong));

    static NativeEventBase() => *Value = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteOne() => Write(Value, sizeof(ulong));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Read()
    {
        Unsafe.SkipInit(out ulong buffer);
        Read(&buffer, sizeof(ulong));
    }
}