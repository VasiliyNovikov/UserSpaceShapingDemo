using System;
using System.IO;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed unsafe class NativeFile(string path, NativeFileFlags flags, UnixFileMode mode = UnixFileMode.None)
    : FileObject(LibC.open(path, flags, mode).ThrowIfError())
{
    private bool _immutableCached;
    private ulong _deviceId;
    private ulong _iNode;

    public long Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Stat(out var stat);
            return stat.st_size;
        }
    }

    public ulong DeviceId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureImmutableCached();
            return _deviceId;
        }
    }

    public ulong INode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureImmutableCached();
            return _iNode;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(Span<byte> buffer)
    {
        fixed (byte* ptr = buffer)
            return (int)base.Read(ptr, (nuint)buffer.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Write(ReadOnlySpan<byte> buffer)
    {
        fixed (byte* ptr = buffer)
            return (int)base.Write(ptr, (nuint)buffer.Length);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureImmutableCached()
    {
        if (_immutableCached)
            return;
        Stat(out var stat);
        _deviceId = stat.st_dev;
        _iNode = stat.st_ino;
        _immutableCached = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Stat(out LibC.stat buf) => LibC.fstat(Descriptor, out buf).ThrowIfError();
}