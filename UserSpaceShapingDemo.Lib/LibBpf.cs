#pragma warning disable IDE1006
using System;
using System.Runtime.InteropServices;

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