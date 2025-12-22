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
    protected nint Write(void* buffer, nuint count) => LibC.write(descriptor, buffer, count).ThrowIfError();

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
}