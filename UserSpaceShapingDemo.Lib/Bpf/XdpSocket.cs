using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Bpf;

public sealed unsafe class XdpSocket : NativeObject
{
    private readonly LibBpf.xsk_socket* _xsk;

    public XdpSocket(string ifName,
                     uint queueId,
                     UMemory umem,
                     RxRingBuffer rxRing,
                     TxRingBuffer txRing,
                     uint rxSize,
                     uint txSize,
                     XdpSocketMode mode,
                     XdpSocketBindMode bindMode)
    {
        var ifIndex = InterfaceNameHelper.GetIndex(ifName);
        var config = new LibBpf.xsk_socket_config
        {
            rx_size = rxSize,
            tx_size = txSize,
            libbpf_flags = 0,
            xdp_flags = (uint)mode,
            bind_flags = (ushort)bindMode,
        };
        LibBpf.xsk_socket__create(out _xsk, ifName, queueId, umem.UMem, ref rxRing.Ring, ref txRing.Ring, config).ThrowIfError();
        XdpProgram.GetMap(ifIndex, out var mapFd);
        LibBpf.xsk_socket__update_xskmap(_xsk, mapFd).ThrowIfError();
    }

    protected override void ReleaseUnmanagedResources()
    {
        if (_xsk is not null)
            LibBpf.xsk_socket__delete(_xsk);
    }
}