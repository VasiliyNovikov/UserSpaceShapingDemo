using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Bpf;

public sealed unsafe class UMemory : NativeObject
{
    private readonly void* _mem;
    private readonly LibBpf.xsk_umem* _umem;

    internal LibBpf.xsk_umem* UMem
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _umem;
    }

    public UMemory(FillRingBuffer fillRing,
                   CompletionRingBuffer completionRing,
                   uint frameCount,
                   uint frameSize = LibBpf.XSK_UMEM__DEFAULT_FRAME_SIZE,
                   uint fillRingSize = LibBpf.XSK_RING_CONS__DEFAULT_NUM_DESCS,
                   uint completionRingSize = LibBpf.XSK_RING_PROD__DEFAULT_NUM_DESCS,
                   uint frameHeadRoom = LibBpf.XSK_UMEM__DEFAULT_FRAME_HEADROOM)
    {
        var size = (ulong)frameCount * frameSize;
        _mem = NativeMemory.AlignedAlloc((nuint)size, (nuint)Environment.SystemPageSize);

        var config = new LibBpf.xsk_umem_config
        {
            fill_size = fillRingSize,
            comp_size = completionRingSize,
            frame_size = frameSize,
            frame_headroom = frameHeadRoom,
            flags = LibBpf.XSK_UMEM__DEFAULT_FLAGS
        };
        var error = LibBpf.xsk_umem__create(out _umem, _mem, size, out fillRing.Ring, out completionRing.Ring, config);
        if (error != 0)
        {
            NativeMemory.AlignedFree(_mem);
            throw new Win32Exception(-error);
        }
    }

    protected override void ReleaseUnmanagedResources()
    {
        try
        {
            if (_umem is not null)
            {
                var error = LibBpf.xsk_umem__delete(_umem);
                if (error != 0)
                    throw new Win32Exception(-error);
            }
        }
        finally
        {
            if (_mem is not null)
                NativeMemory.AlignedFree(_mem);
        }
    }
}