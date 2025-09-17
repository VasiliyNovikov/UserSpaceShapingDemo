using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Bpf;

public sealed unsafe class XdpSocket : CriticalFinalizerObject, IDisposable
{
    private readonly LibBpf.xsk_socket* _xsk;

    public XdpSocket(string ifname,
                     uint queueId,
                     UMemory umem,
                     RxRingBuffer rxRing,
                     TxRingBuffer txRing,
                     uint rxSize,
                     uint txSize,
                     XdpSocketMode mode,
                     XdpSocketBindMode bindMode)
    {
        var config = new LibBpf.xsk_socket_config
        {
            rx_size = rxSize,
            tx_size = txSize,
            libbpf_flags = 0,
            xdp_flags = (uint)mode,
            bind_flags = (ushort)bindMode,
        };
        var error = LibBpf.xsk_socket__create(out _xsk, ifname, queueId, umem.UMem, ref rxRing.Ring, ref txRing.Ring, config);
        if (error != 0)
            throw new Win32Exception(-error);
    }

    private void ReleaseUnmanagedResources()
    {
        if (_xsk is not null)
            LibBpf.xsk_socket__delete(_xsk);
    }

    ~XdpSocket() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}