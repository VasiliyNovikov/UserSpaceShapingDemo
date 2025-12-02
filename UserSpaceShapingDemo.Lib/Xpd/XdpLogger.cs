using System;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public static class XdpLogger
{
    private static LibBpf.libbpf_print_fn_t? _nativeLogger;

    public static unsafe void SetLogger(Action<XdpLogLevel, string> logger)
    {
        _nativeLogger = (level, format, args) =>
        {
            logger((XdpLogLevel)level, NativeString.Format(format, args).TrimEnd('\n'));
            return 0;
        };
        LibBpf.libbpf_set_print(_nativeLogger);
    }
}