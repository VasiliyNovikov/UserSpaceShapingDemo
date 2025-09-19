using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

[Flags]
public enum XdpSocketBindMode : ushort
{
    None = 0,
    SharedUMem = LibBpf.XDP_SHARED_UMEM,
    Copy = LibBpf.XDP_COPY,
    ZeroCopy = LibBpf.XDP_ZEROCOPY,
    UseNeedWakeup = LibBpf.XDP_USE_NEED_WAKEUP
}