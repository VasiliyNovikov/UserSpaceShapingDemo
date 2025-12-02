using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public enum XdpLogLevel
{
    Warning = LibBpf.libbpf_print_level.LIBBPF_WARN,
    Information = LibBpf.libbpf_print_level.LIBBPF_INFO,
    Debug = LibBpf.libbpf_print_level.LIBBPF_DEBUG,
}