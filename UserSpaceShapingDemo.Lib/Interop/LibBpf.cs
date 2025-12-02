using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe partial class LibBpf
{
    internal const string Lib = "libbpf";

    // typedef int (*libbpf_print_fn_t)(enum libbpf_print_level level, const char *, va_list ap);
    public delegate int libbpf_print_fn_t(libbpf_print_level level, byte* format, void* ap);

    // LIBBPF_API libbpf_print_fn_t libbpf_set_print(libbpf_print_fn_t fn);
    [LibraryImport(Lib, EntryPoint = "libbpf_set_print")]
    public static partial libbpf_print_fn_t libbpf_set_print(libbpf_print_fn_t fn);

    public enum libbpf_print_level
    {
        LIBBPF_WARN,
        LIBBPF_INFO,
        LIBBPF_DEBUG,
    }
}