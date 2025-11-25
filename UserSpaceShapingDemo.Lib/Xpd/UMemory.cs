using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed unsafe class UMemory : NativeObject, IFileObject
{
    private readonly void* _umem_area;
    private readonly LibBpf.xsk_umem* _umem;

    internal LibBpf.xsk_umem* UMem
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _umem;
    }

    public uint FrameCount { get; }
    public uint FrameSize { get; }
    public uint FillRingSize { get; }
    public uint CompletionRingSize { get; }

    public FillRingBuffer FillRing { get; } = new();
    public CompletionRingBuffer CompletionRing { get; } = new();

    public FileDescriptor Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LibBpf.xsk_umem__fd(_umem);
    }

    public void* this[ulong address]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LibBpf.xsk_umem__get_data(_umem_area, address);
    }

    public Span<byte> this[in XdpDescriptor packet]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this[packet.Address], (int)packet.Length);
    }

    public UMemory(uint frameCount = LibBpf.XSK_RING_CONS__DEFAULT_NUM_DESCS * 2,
                   uint frameSize = LibBpf.XSK_UMEM__DEFAULT_FRAME_SIZE,
                   uint fillRingSize = LibBpf.XSK_RING_CONS__DEFAULT_NUM_DESCS,
                   uint completionRingSize = LibBpf.XSK_RING_PROD__DEFAULT_NUM_DESCS,
                   uint frameHeadRoom = LibBpf.XSK_UMEM__DEFAULT_FRAME_HEADROOM)
    {
        FrameCount = frameCount;
        FrameSize = frameSize;
        FillRingSize = fillRingSize;
        CompletionRingSize = completionRingSize;
        var size = (ulong)frameCount * frameSize;
        _umem_area = NativeMemory.AlignedAlloc((nuint)size, (nuint)Environment.SystemPageSize);

        var config = new LibBpf.xsk_umem_config
        {
            fill_size = fillRingSize,
            comp_size = completionRingSize,
            frame_size = frameSize,
            frame_headroom = frameHeadRoom,
            flags = LibBpf.XSK_UMEM__DEFAULT_FLAGS
        };
        try
        {
            LibBpf.xsk_umem__create(out _umem, _umem_area, size, out FillRing.Ring, out CompletionRing.Ring, config).ThrowIfError();
        }
        catch
        {
            NativeMemory.AlignedFree(_umem_area);
            throw;
        }
    }

    public void GetAddresses(Span<ulong> addresses)
    {
        if ((uint)addresses.Length != FrameCount)
            throw new ArgumentOutOfRangeException(nameof(addresses));
        ref var address = ref MemoryMarshal.GetReference(addresses);
        for (var i = 0ul; i < FrameCount; ++i)
        {
            address = i * FrameSize;
            address = ref Unsafe.Add(ref address, 1);
        }
    }

    protected override void ReleaseUnmanagedResources()
    {
        try
        {
            if (_umem is not null)
                LibBpf.xsk_umem__delete(_umem).ThrowIfError();
        }
        finally
        {
            if (_umem_area is not null)
                NativeMemory.AlignedFree(_umem_area);
        }
    }
}