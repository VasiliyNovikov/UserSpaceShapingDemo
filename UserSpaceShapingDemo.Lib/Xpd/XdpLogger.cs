using System;

using LinuxCore;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public static class XdpLogger
{
    private static LibBpf.libbpf_print_fn_t? _nativeLogger;

    public static unsafe void SetLogger(Action<XdpLogLevel, string> logger)
    {
        _nativeLogger = (level, format, args) =>
        {
            logger((XdpLogLevel)level, LinuxString.Format(format, args, 128).TrimEnd('\n'));
            return 0;
        };
        LibBpf.libbpf_set_print(_nativeLogger);
    }
}