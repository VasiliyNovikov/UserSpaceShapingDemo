using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib;

public abstract unsafe class RingBuffer : CriticalFinalizerObject, IDisposable
{
    private static readonly nuint Size = (nuint)sizeof(LibBpf.xsk_ring);

    private readonly LibBpf.xsk_ring* _ring;

    internal ref LibBpf.xsk_ring Ring
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *_ring;
    }

    protected RingBuffer()
    {
        _ring = (LibBpf.xsk_ring*)NativeMemory.AlignedAlloc(Size, (nuint)IntPtr.Size);
        NativeMemory.Clear(_ring, Size);
    }

    private void ReleaseUnmanagedResources() => NativeMemory.AlignedFree(_ring);

    ~RingBuffer() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref ulong Address(uint idx) => ref LibBpf.xsk_ring__addr(ref Ring, idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref XdpDescriptor Descriptor(uint idx) => ref Unsafe.As<LibBpf.xdp_desc, XdpDescriptor>(ref LibBpf.xsk_ring__desc(ref Ring, idx));
}