using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib;

public sealed unsafe class UMemory : CriticalFinalizerObject, IDisposable
{
    private readonly void* _mem;
    private readonly void* _umem;

    public UMemory(ProducerRingBuffer fillRing,
                   ConsumerRingBuffer completionRing,
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

    private void ReleaseUnmanagedResources()
    {
        if (_umem is not null)
            LibBpf.xsk_umem__delete(_umem);
        if (_mem is not null)
            NativeMemory.AlignedFree(_mem);
    }

    ~UMemory() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}