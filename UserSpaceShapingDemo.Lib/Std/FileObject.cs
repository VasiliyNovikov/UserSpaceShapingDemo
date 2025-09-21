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

    protected override void ReleaseUnmanagedResources() => LibC.close(Descriptor).ThrowIfError();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected nint Read(void* buffer, nuint count) => LibC.read(Descriptor, buffer, count).ThrowIfError();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected nint Write(void* buffer, nuint count) => LibC.write(Descriptor, buffer, count).ThrowIfError();
}