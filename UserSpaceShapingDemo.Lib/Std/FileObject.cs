using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public abstract unsafe class FileObject(FileDescriptor descriptor) : NativeObject, IFileObject
{
    public FileDescriptor Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => descriptor;
    }

    protected override void ReleaseUnmanagedResources() => LibC.close(descriptor).ThrowIfError();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected nint Read(void* buffer, nuint count) => LibC.read(descriptor, buffer, count).ThrowIfError();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryRead(void* buffer, nuint count, out nint readCount) => TryComplete(LibC.write(descriptor, buffer, count), out readCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected nint Write(void* buffer, nuint count) => LibC.write(descriptor, buffer, count).ThrowIfError();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryWrite(void* buffer, nuint count, out nint writtenCount) => TryComplete(LibC.write(descriptor, buffer, count), out writtenCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IOCctl(ulong request, void* arg) => LibC.ioctl(descriptor, request, arg).ThrowIfError();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IOCctl(ulong request, ulong arg) => IOCctl(request, (void*)arg);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IOCctl<T>(ulong request, ref T arg) where T : unmanaged
    {
        fixed (T* pArg = &arg)
            return IOCctl(request, pArg);
    }

    private static bool TryComplete(nint result, out nint count)
    {
        if (result.IsError())
        {
            var error = NativeErrorNumber.Last;
            if (error is NativeErrorNumber.TryAgain or NativeErrorNumber.OperationWouldBlock or NativeErrorNumber.InterruptedSystemCall)
            {
                count = 0;
                return false;
            }
            throw new NativeException(error);
        }
        count = result;
        return true;
    }
}