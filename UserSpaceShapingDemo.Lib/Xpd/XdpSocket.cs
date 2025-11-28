using System;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed unsafe class XdpSocket : NativeObject, IFileObject
{
    private readonly LibXdp.xsk_socket* _xsk;
    private FileDescriptor? _descriptor;

    public UMemory Umem { get; }
    public string IfName { get; }

    public FillRingBuffer FillRing { get; }
    public CompletionRingBuffer CompletionRing { get; }
    public RxRingBuffer RxRing { get; } = new();
    public TxRingBuffer TxRing { get; } = new();

    public FileDescriptor Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _descriptor ??= LibXdp.xsk_socket__fd(_xsk);
    }

    public XdpSocket(UMemory umem,
                     string ifName,
                     uint queueId = 0,
                     uint rxSize = LibXdp.XSK_RING_CONS__DEFAULT_NUM_DESCS,
                     uint txSize = LibXdp.XSK_RING_PROD__DEFAULT_NUM_DESCS,
                     XdpSocketMode mode = XdpSocketMode.Default,
                     XdpSocketBindMode bindMode = XdpSocketBindMode.Copy | XdpSocketBindMode.UseNeedWakeup,
                     bool shared = false)
    {
        Umem = umem;
        IfName = ifName;
        var ifIndex = InterfaceNameHelper.GetIndex(ifName);
        var config = new LibXdp.xsk_socket_config
        {
            rx_size = rxSize,
            tx_size = txSize,
            libbpf_flags = 0,
            xdp_flags = (uint)mode,
            bind_flags = (ushort)bindMode,
        };
        if (shared)
        {
            config.bind_flags |= LibXdp.XDP_SHARED_UMEM;
            FillRing = new();
            CompletionRing = new();
            LibXdp.xsk_socket__create_shared(out _xsk, ifName, queueId, umem.UMem, out RxRing.Ring, out TxRing.Ring, out FillRing.Ring, out CompletionRing.Ring, config).ThrowIfError();
        }
        else
        {
            FillRing = umem.FillRing;
            CompletionRing = umem.CompletionRing;
            LibXdp.xsk_socket__create(out _xsk, ifName, queueId, umem.UMem, out RxRing.Ring, out TxRing.Ring, config).ThrowIfError();
        }

        XdpProgram.GetMap(ifIndex, out var mapDescriptor);
        LibXdp.xsk_socket__update_xskmap(_xsk, mapDescriptor).ThrowIfError();
    }

    public bool WaitFor(Poll.Event events, NativeCancellationToken cancellationToken) => cancellationToken.Wait(this, events);

    public static bool WaitFor(ReadOnlySpan<XdpSocket> sockets, ReadOnlySpan<Poll.Event> events, NativeCancellationToken cancellationToken) => cancellationToken.Wait(sockets, events);

    public void WakeUp() => LibC.sendto(Descriptor, null, 0, LibC.MSG_DONTWAIT, null, 0).ThrowIfError();

    protected override void ReleaseUnmanagedResources()
    {
        if (_xsk is not null)
            LibXdp.xsk_socket__delete(_xsk);
    }
}