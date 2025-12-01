using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibXdp
{
    private const string Lib = "libxdp";
    private const string LegacyLib = "libbpf";

    private static bool? IsLegacyLibValue;

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

    public static bool IsLegacyLib
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (IsLegacyLibValue is { } value)
                return value;

            xsk_umem__delete(null);
            return IsLegacyLibValue!.Value;
        }
    }

    static LibXdp()
    {
        NativeLibrary.SetDllImportResolver(typeof(LibXdp).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName != Lib)
                return IntPtr.Zero;

            if (NativeLibrary.TryLoad(Lib, assembly, searchPath, out var handle))
            {
                IsLegacyLibValue = false;
                return handle;
            }

            if (NativeLibrary.TryLoad(LegacyLib, assembly, searchPath, out handle))
            {
                IsLegacyLibValue = true;
                return handle;
            }

            throw new DllNotFoundException($"Could not load {Lib} or {LegacyLib} for AF_XDP");
        });
    }

    // Raw P/Invoke where config may be NULL (defaults applied by libbpf)
    // int xsk_umem__create(struct xsk_umem **umem, void *area, __u64 size,
    //                      struct xsk_ring_prod *fill, struct xsk_ring_cons *comp,
    //                      const struct xsk_umem_config *config);
    [LibraryImport(Lib, EntryPoint = "xsk_umem__create")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial xsk_api_result xsk_umem__create(out xsk_umem* umem,
                                                          void* umem_area,
                                                          ulong size,
                                                          out xsk_ring fill,
                                                          out xsk_ring comp,
                                                          in xsk_umem_config config);

    // int xsk_umem__delete(struct xsk_umem *umem);
    // Returns 0 on success, -EBUSY if the UMEM is still in use (per xsk.h).
    [LibraryImport(Lib, EntryPoint = "xsk_umem__delete")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial xsk_api_result xsk_umem__delete(xsk_umem* umem);

    // int xsk_umem__fd(const struct xsk_umem *umem)
    [LibraryImport(Lib, EntryPoint = "xsk_umem__fd")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial FileDescriptor xsk_umem__fd(xsk_umem* umem);

    // Port of libbpf's void *xsk_umem__get_data(void *umem_area, __u64 addr)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* xsk_umem__get_data(void* umem_area, ulong addr) => &((byte*)umem_area)[addr];

    // int xsk_setup_xdp_prog(int ifindex, int *xsks_map_fd)
    [LibraryImport(Lib, EntryPoint = "xsk_setup_xdp_prog")]
    public static partial xsk_api_result xsk_setup_xdp_prog(int ifindex, out FileDescriptor xsks_map_fd);

    // LIBBPF_API int xsk_socket__create(struct xsk_socket **xsk,
    //                                   const char *ifname, __u32 queue_id,
    //                                   struct xsk_umem *umem,
    //                                   struct xsk_ring_cons *rx,
    //                                   struct xsk_ring_prod *tx,
    //                                   const struct xsk_socket_config *config);
    [LibraryImport(Lib, EntryPoint = "xsk_socket__create", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial xsk_api_result xsk_socket__create(out xsk_socket* xsk,
                                                            string ifname,
                                                            uint queue_id,
                                                            xsk_umem* umem,
                                                            out xsk_ring rx,
                                                            out xsk_ring tx,
                                                            in xsk_socket_config config);

    // LIBBPF_API int
    // xsk_socket__create_shared(struct xsk_socket **xsk_ptr,
    //                           const char *ifname,
    //                           __u32 queue_id, struct xsk_umem *umem,
    //                           struct xsk_ring_cons *rx,
    //                           struct xsk_ring_prod *tx,
    //                           struct xsk_ring_prod *fill,
    //                           struct xsk_ring_cons *comp,
    //                           const struct xsk_socket_config *config);
    [LibraryImport(Lib, EntryPoint = "xsk_socket__create_shared", StringMarshalling = StringMarshalling.Utf8)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial xsk_api_result xsk_socket__create_shared(out xsk_socket* xsk,
                                                                   string ifname,
                                                                   uint queue_id,
                                                                   xsk_umem* umem,
                                                                   out xsk_ring rx,
                                                                   out xsk_ring tx,
                                                                   out xsk_ring fill,
                                                                   out xsk_ring comp,
                                                                   in xsk_socket_config config);

    // LIBBPF_API void xsk_socket__delete(struct xsk_socket *xsk);
    [LibraryImport(Lib, EntryPoint = "xsk_socket__delete")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial void xsk_socket__delete(xsk_socket* xsk);

    // int xsk_socket__update_xskmap(struct xsk_socket *xsk, int xsks_map_fd);
    [LibraryImport(Lib, EntryPoint = "xsk_socket__update_xskmap")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial xsk_api_result xsk_socket__update_xskmap(xsk_socket* xsk, FileDescriptor xsks_map_fd);

    // int xsk_socket__fd(const struct xsk_socket *xsk)
    [LibraryImport(Lib, EntryPoint = "xsk_socket__fd")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressGCTransition]
    public static partial FileDescriptor xsk_socket__fd(xsk_socket* xsk);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_cons_nb_avail_cached(ref xsk_ring r) => r.cached_prod - r.cached_cons; // (mod 2^32 arithmetic)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_cons_nb_avail_aggressive(ref xsk_ring r) => (r.cached_prod = Volatile.Read(ref *r.producer)) - r.cached_cons; // Refresh cached_prod with acquire semantics

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_cons_nb_avail_relaxed(ref xsk_ring r)
    {
        var entries = xsk_cons_nb_avail_cached(ref r);
        return entries == 0 ? xsk_cons_nb_avail_aggressive(ref r) : entries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_cons_nb_avail_relaxed(ref xsk_ring r, uint nb) => Math.Min(xsk_cons_nb_avail_relaxed(ref r), nb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_cons_nb_avail_aggressive(ref xsk_ring r, uint nb) => xsk_cons_nb_avail_cached(ref r) >= nb ? nb : Math.Min(xsk_cons_nb_avail_aggressive(ref r), nb);

    // Port of libbpf's xsk_ring_cons__peek().
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_ring_cons__peek_complete(ref xsk_ring cons, uint entries, out uint idx)
    {
        if (entries == 0)
            Unsafe.SkipInit(out idx);
        else
        {
            idx = cons.cached_cons;
            cons.cached_cons += entries;
        }
        return entries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_cons__peek_relaxed(ref xsk_ring cons, uint nb, out uint idx) => xsk_ring_cons__peek_complete(ref cons, xsk_cons_nb_avail_relaxed(ref cons, nb), out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_cons__peek_aggressive(ref xsk_ring cons, uint nb, out uint idx) => xsk_ring_cons__peek_complete(ref cons, xsk_cons_nb_avail_aggressive(ref cons, nb), out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_cons__peek_relaxed(ref xsk_ring cons, out uint idx) => xsk_ring_cons__peek_complete(ref cons, xsk_cons_nb_avail_relaxed(ref cons), out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_cons__peek_aggressive(ref xsk_ring cons, out uint idx) => xsk_ring_cons__peek_complete(ref cons, xsk_cons_nb_avail_aggressive(ref cons), out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool xsk_ring_prod__needs_wakeup(in xsk_ring r) => (*r.flags & XDP_RING_NEED_WAKEUP) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_prod_nb_free_cached(ref xsk_ring r) => r.cached_cons - r.cached_prod;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_prod_nb_free_aggressive(ref xsk_ring r) => (r.cached_cons = Volatile.Read(ref *r.consumer) + r.size) - r.cached_prod;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_prod_nb_free_aggressive(ref xsk_ring r, uint nb) => xsk_prod_nb_free_cached(ref r) >= nb ? nb : Math.Min(xsk_prod_nb_free_aggressive(ref r), nb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_prod_nb_free_relaxed(ref xsk_ring r, uint nb)
    {
        var free_entries = xsk_prod_nb_free_cached(ref r);
        return Math.Min(free_entries >= 0 ? free_entries : xsk_prod_nb_free_aggressive(ref r), nb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_prod__reserve_complete(ref xsk_ring prod, uint free_entries, out uint idx)
    {
        if (free_entries == 0)
            Unsafe.SkipInit(out idx);
        else
        {
            idx = prod.cached_prod;
            prod.cached_prod += free_entries;
        }
        return free_entries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_prod__reserve_relaxed(ref xsk_ring prod, uint nb, out uint idx) => xsk_ring_prod__reserve_complete(ref prod, xsk_prod_nb_free_relaxed(ref prod, nb), out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint xsk_ring_prod__reserve_aggressive(ref xsk_ring prod, uint nb, out uint idx) => xsk_ring_prod__reserve_complete(ref prod, xsk_prod_nb_free_aggressive(ref prod, nb), out idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void xsk_ring_advance(uint* ring, uint nb) => Volatile.Write(ref *ring, *ring + nb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void xsk_ring_cons__release(ref xsk_ring cons, uint nb) => xsk_ring_advance(cons.consumer, nb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void xsk_ring_prod__submit(ref xsk_ring prod, uint nb) => xsk_ring_advance(prod.producer, nb);

    // Address helpers (ports of libbpf's xsk_ring_cons__comp_addr() and xsk_ring_prod__fill_addr())
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref ulong xsk_ring__addr(ref xsk_ring ring, uint idx) => ref ((ulong*)ring.ring)[idx & ring.mask];

    // Port of libbpf's xsk_ring_cons__rx_desc() and xsk_ring_prod__tx_desc()
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref xdp_desc xsk_ring__desc(ref xsk_ring ring, uint idx) => ref ((xdp_desc*)ring.ring)[idx & ring.mask];

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct xsk_api_result { public readonly int error_code; }

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
    public readonly struct xsk_umem;

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
    public readonly struct xsk_socket;
}