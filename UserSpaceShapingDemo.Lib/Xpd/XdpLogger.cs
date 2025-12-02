using System;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Xpd;

public static class XdpLogger
{
    public static unsafe void SetLogger(Action<XdpLogLevel, string> logger)
    {
        LibBpf.libbpf_set_print((level, format, args) =>
        {
            logger((XdpLogLevel)level, NativeString.Format(format, args));
            return 0;
        });
    }
}