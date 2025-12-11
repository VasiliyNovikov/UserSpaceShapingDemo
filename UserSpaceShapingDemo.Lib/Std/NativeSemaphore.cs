using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed unsafe class NativeSemaphore(uint initialValue = 0)
    : FileObject(LibC.eventfd(initialValue, LibC.EFD_SEMAPHORE).ThrowIfError())
{
    private static readonly long* Value = (long*)NativeMemory.Alloc(sizeof(ulong));
    private static readonly long* Buffer = (long*)NativeMemory.Alloc(sizeof(ulong));

    static NativeSemaphore() => *Value = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment() => Write(Value, sizeof(ulong));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ulong value) => Write(&value, sizeof(ulong));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Decrement() => Read(Buffer, sizeof(ulong));
}