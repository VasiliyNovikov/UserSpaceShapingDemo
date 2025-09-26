using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed unsafe class XdpSocket : NativeObject, IFileObject
{
    private readonly LibBpf.xsk_socket* _xsk;
    private FileDescriptor? _descriptor;

    public UMemory Umem { get; }

    public RxRingBuffer RxRing { get; } = new();
    public TxRingBuffer TxRing { get; } = new();

    public FileDescriptor Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _descriptor ??= LibBpf.xsk_socket__fd(_xsk);
    }

    public XdpSocket(UMemory umem,
                     string ifName,
                     uint queueId = 0,
                     uint rxSize = LibBpf.XSK_RING_CONS__DEFAULT_NUM_DESCS,
                     uint txSize = LibBpf.XSK_RING_PROD__DEFAULT_NUM_DESCS,
                     XdpSocketMode mode = XdpSocketMode.Default,
                     XdpSocketBindMode bindMode = XdpSocketBindMode.Copy | XdpSocketBindMode.UseNeedWakeup)
    {
        Umem = umem;
        var ifIndex = InterfaceNameHelper.GetIndex(ifName);
        var config = new LibBpf.xsk_socket_config
        {
            rx_size = rxSize,
            tx_size = txSize,
            libbpf_flags = 0,
            xdp_flags = (uint)mode,
            bind_flags = (ushort)bindMode,
        };
        LibBpf.xsk_socket__create(out _xsk, ifName, queueId, umem.UMem, out RxRing.Ring, out TxRing.Ring, config).ThrowIfError();
        XdpProgram.GetMap(ifIndex, out var mapDescriptor);
        LibBpf.xsk_socket__update_xskmap(_xsk, mapDescriptor).ThrowIfError();
    }

    public void WaitForRead(NativeCancellationToken cancellationToken) => cancellationToken.WaitRead(Descriptor);

    public void WaitForWrite(NativeCancellationToken cancellationToken) => cancellationToken.WaitWrite(Descriptor);

    protected override void ReleaseUnmanagedResources()
    {
        if (_xsk is not null)
            LibBpf.xsk_socket__delete(_xsk);
    }
}