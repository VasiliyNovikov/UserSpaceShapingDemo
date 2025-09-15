#pragma warning disable IDE1006
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace UserSpaceShapingDemo.Lib;

internal static unsafe partial class LibBpf
{
    private const string Lib = "libbpf";

    // Raw P/Invoke where config may be NULL (defaults applied by libbpf)
    // int xsk_umem__create(struct xsk_umem **umem, void *area, __u64 size,
    //                      struct xsk_ring_prod *fill, struct xsk_ring_cons *comp,
    //                      const struct xsk_umem_config *config);
    [LibraryImport(Lib, EntryPoint = "xsk_umem__create", SetLastError = true)]
    public static partial int xsk_umem__create(
        out IntPtr umem,
        IntPtr umem_area,
        ulong size,
        out xsk_ring_prod fill,
        out xsk_ring_cons comp,
        IntPtr config = 0 /* pass IntPtr.Zero to use libbpf defaults */);

    // Overload that pins & passes a config by ref
    [LibraryImport(Lib, EntryPoint = "xsk_umem__create", SetLastError = true)]
    public static partial int xsk_umem__create(
        out IntPtr umem,
        IntPtr umem_area,
        ulong size,
        out xsk_ring_prod fill,
        out xsk_ring_cons comp,
        in xsk_umem_config config);

    // int xsk_umem__delete(struct xsk_umem *umem);
    // Returns 0 on success, -EBUSY if the UMEM is still in use (per xsk.h).
    [LibraryImport(Lib, EntryPoint = "xsk_umem__delete", SetLastError = true)]
    public static partial int xsk_umem__delete(IntPtr umem);

    // C# port of:
    // static inline __u32 xsk_cons_nb_avail(struct xsk_ring_cons *r, __u32 nb)
    // and
    // static inline __u32 xsk_ring_cons__peek(struct xsk_ring_cons *cons, __u32 nb, __u32 *idx)
    //
    // Source: tools/lib/bpf/xsk.h (libbpf)
    // - xsk_cons_nb_avail(): entries = cached_prod - cached_cons; refresh cached_prod with load-acquire when 0
    // - xsk_ring_cons__peek(): returns min(nb, avail), writes *idx = cached_cons, bumps cached_cons by entries

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint LoadAcquire(uint* p)
    {
        // Equivalent to libbpf_smp_load_acquire: do the load, then an acquire barrier
        uint v = *p;
        Thread.MemoryBarrier(); // acquire fence
        return v;
    }

    // If you also want to port xsk_ring_cons__release():
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void xsk_ring_cons__release(ref xsk_ring_cons cons, uint nb)
    {
        // libbpf does store-release to *consumer += nb
        Thread.MemoryBarrier(); // release fence
        uint* consumer = cons.consumer;
        *consumer = unchecked(*consumer + nb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint xsk_cons_nb_avail(ref xsk_ring_cons r, uint nb)
    {
        // entries = r.cached_prod - r.cached_cons; (mod 2^32 arithmetic)
        uint entries = unchecked(r.cached_prod - r.cached_cons);
        if (entries == 0)
        {
            // Refresh cached_prod with acquire semantics
            r.cached_prod = LoadAcquire(r.producer);
            entries = unchecked(r.cached_prod - r.cached_cons);
        }
        return entries > nb ? nb : entries;
    }

    // Port of libbpf's xsk_ring_cons__peek().
    // Returns number of entries granted (<= nb). When > 0, idx is set to the starting ring index.
    // NOTE: idx here is the absolute ring index (cached_cons). Masking to the ring is done by the
    // address helpers (e.g., xsk_ring_cons__comp_addr / __rx_desc) just like libbpf.
    public static uint xsk_ring_cons__peek(ref xsk_ring_cons cons, uint nb, out uint idx)
    {
        uint entries = xsk_cons_nb_avail(ref cons, nb);
        if (entries > 0)
        {
            idx = cons.cached_cons;
            cons.cached_cons = unchecked(cons.cached_cons + entries);
            return entries;
        }

        idx = 0;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_umem_config
    {
        public uint fill_size;
        public uint comp_size;
        public uint frame_size;
        public uint frame_headroom;
        public uint flags;

        // Handy factory using libbpf defaults from xsk.h
        public static xsk_umem_config Default(
            uint? fillSize = null,
            uint? compSize = null,
            uint? frameSize = null,
            uint? frameHeadroom = null,
            uint? flags = null)
        {
            return new xsk_umem_config
            {
                fill_size = fillSize ?? XSK_RING_CONS__DEFAULT_NUM_DESCS,
                comp_size = compSize ?? XSK_RING_PROD__DEFAULT_NUM_DESCS,
                frame_size = frameSize ?? XSK_UMEM__DEFAULT_FRAME_SIZE,
                frame_headroom = frameHeadroom ?? XSK_UMEM__DEFAULT_FRAME_HEADROOM,
                flags = flags ?? XSK_UMEM__DEFAULT_FLAGS
            };
        }

        // Defaults (from xsk.h)
        public const uint XSK_RING_CONS__DEFAULT_NUM_DESCS = 2048;
        public const uint XSK_RING_PROD__DEFAULT_NUM_DESCS = 2048;
        public const uint XSK_UMEM__DEFAULT_FRAME_SIZE = 4096;
        public const uint XSK_UMEM__DEFAULT_FRAME_HEADROOM = 0;
        public const uint XSK_UMEM__DEFAULT_FLAGS = 0;
    }

    // Mirrors: DEFINE_XSK_RING(xsk_ring_prod);
    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_ring_prod
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

    // Mirrors: DEFINE_XSK_RING(xsk_ring_cons);
    [StructLayout(LayoutKind.Sequential)]
    public struct xsk_ring_cons
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
}