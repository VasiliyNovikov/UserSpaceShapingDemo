using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibBpf
{
    private const string Lib = "libbpf";

    public const ushort XDP_SHARED_UMEM = 0b0001;
    public const ushort XDP_COPY        = 0b0010; // Force copy-mode
    public const ushort XDP_ZEROCOPY    = 0b0100; // Force zero-copy mode
    /* If this option is set, the driver might go sleep and in that case
     * the XDP_RING_NEED_WAKEUP flag in the fill and/or Tx rings will be
     * set. If it is set, the application need to explicitly wake up the
     * driver with a poll() (Rx and Tx) or sendto() (Tx only). If you are
     * running the driver and the application on the same core, you should
     * use this option so that the kernel will yield to the user space
     * application.
     */
    public const ushort XDP_USE_NEED_WAKEUP = 0b1000;

    public const uint XDP_RING_NEED_WAKEUP = 1;
    public const uint XSK_RING_CONS__DEFAULT_NUM_DESCS = 2048;
    public const uint XSK_RING_PROD__DEFAULT_NUM_DESCS = 2048;

    public const uint XSK_UMEM__DEFAULT_FRAME_SIZE = 4096;
    public const uint XSK_UMEM__DEFAULT_FRAME_HEADROOM = 0;
    public const uint XSK_UMEM__DEFAULT_FLAGS = 0;

    public const uint XDP_FLAGS_UPDATE_IF_NOEXIST = 0b0001;
    public const uint XDP_FLAGS_SKB_MODE          = 0b0010;
    public const uint XDP_FLAGS_DRV_MODE          = 0b0100;
    public const uint XDP_FLAGS_HW_MODE           = 0b1000;

    // Raw P/Invoke where config may be NULL (defaults applied by libbpf)
    // int xsk_umem__create(struct xsk_umem **umem, void *area, __u64 size,
    //                      struct xsk_ring_prod *fill, struct xsk_ring_cons *comp,
    //                      const struct xsk_umem_config *config);
    [LibraryImport(Lib, EntryPoint = "xsk_umem__create")]
    public static partial int xsk_umem__create(out xsk_umem* umem,
                                               void* umem_area,
                                               ulong size,
                                               out xsk_ring fill,
                                               out xsk_ring comp,
                                               in xsk_umem_config config);

    // int xsk_umem__delete(struct xsk_umem *umem);
    // Returns 0 on success, -EBUSY if the UMEM is still in use (per xsk.h).
    [LibraryImport(Lib, EntryPoint = "xsk_umem__delete")]
    public static partial int xsk_umem__delete(xsk_umem* umem);

    // LIBBPF_API int xsk_socket__create(struct xsk_socket **xsk,
    //                                   const char *ifname, __u32 queue_id,
    //                                   struct xsk_umem *umem,
    //                                   struct xsk_ring_cons *rx,
    //                                   struct xsk_ring_prod *tx,
    //                                   const struct xsk_socket_config *config);
    [LibraryImport(Lib, EntryPoint = "xsk_socket__create", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int xsk_socket__create(out xsk_socket* xsk,
                                                 string ifname,
                                                 uint queue_id,
                                                 xsk_umem* umem,
                                                 ref xsk_ring rx,
                                                 ref xsk_ring tx,
                                                 in xsk_socket_config config);

    // LIBBPF_API int xsk_socket__create_shared(struct xsk_socket **xsk,
    //                                          const char *ifname, __u32 queue_id,
    //                                          struct xsk_umem *umem,
    //                                          struct xsk_ring_cons *rx,
    //                                          struct xsk_ring_prod *tx,
    //                                          struct xsk_ring_prod *fill,
    //                                          struct xsk_ring_cons *comp,
    //                                          const struct xsk_socket_config *config);
    [LibraryImport(Lib, EntryPoint = "xsk_socket__create_shared", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int xsk_socket__create_shared(out xsk_socket* xsk,
                                                        string ifname,
                                                        uint queue_id,
                                                        xsk_umem* umem,
                                                        ref xsk_ring rx,
                                                        ref xsk_ring tx,
                                                        ref xsk_ring fill,
                                                        ref xsk_ring comp,
                                                        in xsk_socket_config config);

    // LIBBPF_API void xsk_socket__delete(struct xsk_socket *xsk);
    [LibraryImport(Lib, EntryPoint = "xsk_socket__delete")]
    public static partial void xsk_socket__delete(xsk_socket* xsk);

    // C# ports of libbpf's smp_load_acquire() and smp_store_release()
    // Source: tools/lib/bpf/xsk.h
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint libbpf_smp_load_acquire(ref uint p) => Volatile.Read(ref p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void libbpf_smp_store_release(ref uint p, uint v) => Volatile.Write(ref p, v);

    // C# port of:
    // static inline __u32 xsk_cons_nb_avail(struct xsk_ring_cons *r, __u32 nb)
    // and
    // static inline __u32 xsk_ring_cons__peek(struct xsk_ring_cons *cons, __u32 nb, __u32 *idx)
    //
    // Source: tools/lib/bpf/xsk.h (libbpf)
    // - xsk_cons_nb_avail(): entries = cached_prod - cached_cons; refresh cached_prod with load-acquire when 0
    // - xsk_ring_cons__peek(): returns min(nb, avail), writes *idx = cached_cons, bumps cached_cons by entries

    // If you also want to port xsk_ring_cons__release():
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void xsk_ring_cons__release(ref xsk_ring cons, uint nb)
    {
        libbpf_smp_store_release(ref *cons.consumer, *cons.consumer + nb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_cons_nb_avail(ref xsk_ring r, uint nb)
    {
        // entries = r.cached_prod - r.cached_cons; (mod 2^32 arithmetic)
        var entries = r.cached_prod - r.cached_cons;
        if (entries == 0)
        {
            // Refresh cached_prod with acquire semantics
            r.cached_prod = libbpf_smp_load_acquire(ref *r.producer);
            entries = r.cached_prod - r.cached_cons;
        }
        return entries > nb ? nb : entries;
    }

    // Port of libbpf's xsk_ring_cons__peek().
    // Returns number of entries granted (<= nb). When > 0, idx is set to the starting ring index.
    // NOTE: idx here is the absolute ring index (cached_cons). Masking to the ring is done by the
    // address helpers (e.g., xsk_ring_cons__comp_addr / __rx_desc) just like libbpf.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_cons__peek(ref xsk_ring cons, uint nb, out uint idx)
    {
        uint entries = xsk_cons_nb_avail(ref cons, nb);
        if (entries > 0)
        {
            idx = cons.cached_cons;
            cons.cached_cons += entries;
        }
        else
            Unsafe.SkipInit(out idx);

        return entries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool xsk_ring_prod__needs_wakeup(in xsk_ring r) => (*r.flags & XDP_RING_NEED_WAKEUP) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_prod_nb_free(ref xsk_ring r, uint nb)
    {
        var free_entries = r.cached_cons - r.cached_prod;

        if (free_entries >= nb)
            return free_entries;

        /* Refresh the local tail pointer.
         * cached_cons is r->size bigger than the real consumer pointer so
         * that this addition can be avoided in the more frequently
         * executed code that computs free_entries in the beginning of
         * this function. Without this optimization it whould have been
         * free_entries = r->cached_prod - r->cached_cons + r->size.
         */
        r.cached_cons = libbpf_smp_load_acquire(ref *r.consumer);
        r.cached_cons += r.size;

        return r.cached_cons - r.cached_prod;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_prod__reserve(ref xsk_ring prod, uint nb, out uint idx)
    {
        if (xsk_prod_nb_free(ref prod, nb) < nb)
        {
            Unsafe.SkipInit(out idx);
            return 0;
        }

        idx = prod.cached_prod;
        prod.cached_prod += nb;

        return nb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void xsk_ring_prod__submit(ref xsk_ring prod, uint nb)
    {
        /* Make sure everything has been written to the ring before indicating
         * this to the kernel by writing the producer pointer.
         */
        libbpf_smp_store_release(ref *prod.producer, *prod.producer + nb);
    }

    // Address helpers (ports of libbpf's xsk_ring_cons__comp_addr() and xsk_ring_prod__fill_addr())
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref ulong xsk_ring__addr(ref xsk_ring ring, uint idx)
    {
        ulong* addrs = (ulong*)ring.ring;
        return ref addrs[idx & ring.mask];
    }

    // Port of libbpf's xsk_ring_cons__comp_desc() and xsk_ring_prod__fill_desc()
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref xdp_desc xsk_ring__desc(ref xsk_ring ring, uint idx)
    {
        xdp_desc* descs = (xdp_desc*)ring.ring;
        return ref descs[idx & ring.mask];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_umem_config
    {
        public uint fill_size;
        public uint comp_size;
        public uint frame_size;
        public uint frame_headroom;
        public uint flags;
    }

    // Mirrors: DEFINE_XSK_RING(xsk_ring_prod);
    //          DEFINE_XSK_RING(xsk_ring_cons);
    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_ring
    {
        public uint cached_prod;
        public uint cached_cons;
        public uint mask;
        public uint size;
        public uint* producer;
        public uint* consumer;
        public void* ring;
        public uint* flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xdp_desc
    {
        public ulong addr;
        public uint len;
        public uint options;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_umem;

    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_socket_config
    {
        public uint rx_size;
        public uint tx_size;
        public uint libbpf_flags;
        public uint xdp_flags;
        public ushort bind_flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_socket;
}