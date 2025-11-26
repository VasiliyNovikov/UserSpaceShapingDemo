using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public abstract unsafe class RingBuffer : NativeObject
{
    private static readonly nuint Size = (nuint)sizeof(LibXdp.xsk_ring);

    private readonly LibXdp.xsk_ring* _ring;

    internal ref LibXdp.xsk_ring Ring
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *_ring;
    }

    protected RingBuffer()
    {
        _ring = (LibXdp.xsk_ring*)NativeMemory.AlignedAlloc(Size, (nuint)IntPtr.Size);
        NativeMemory.Clear(_ring, Size);
    }

    protected override void ReleaseUnmanagedResources() => NativeMemory.AlignedFree(_ring);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref ulong Address(uint idx) => ref LibXdp.xsk_ring__addr(ref Ring, idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref XdpDescriptor Descriptor(uint idx) => ref Unsafe.As<LibXdp.xdp_desc, XdpDescriptor>(ref LibXdp.xsk_ring__desc(ref Ring, idx));
}