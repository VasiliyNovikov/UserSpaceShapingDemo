using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib;

public abstract class FileObject(FileDescriptor descriptor) : NativeObject, IFileObject
{
    public FileDescriptor Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => descriptor;
    }

    protected override void ReleaseUnmanagedResources()
    {
        if (LibC.close(Descriptor) == -1)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }
}