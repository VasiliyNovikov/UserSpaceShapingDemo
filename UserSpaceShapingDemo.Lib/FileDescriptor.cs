using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FileDescriptor
{
    private readonly int _fd;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal FileDescriptor(int fd) => _fd = fd;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => _fd.ToString(CultureInfo.InvariantCulture);
}

public readonly struct FileDescriptorRef : IDisposable
{
    private readonly SafeHandle _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FileDescriptorRef(SafeHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        bool success = false;
        _handle.DangerousAddRef(ref success);
        if (!success)
            throw new InvalidOperationException("Failed to add reference to handle.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _handle.DangerousRelease();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => ((FileDescriptor)this).ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FileDescriptor(FileDescriptorRef reference) => new(reference._handle.DangerousGetHandle().ToInt32());
}

public static class FileDescriptorRefExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FileDescriptorRef Ref(this SafeHandle handle) => new(handle);
}