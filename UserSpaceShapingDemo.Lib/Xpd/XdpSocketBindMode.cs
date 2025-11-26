using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

[Flags]
public enum XdpSocketBindMode : ushort
{
    None = 0,
    SharedUMem = LibXdp.XDP_SHARED_UMEM,
    Copy = LibXdp.XDP_COPY,
    ZeroCopy = LibXdp.XDP_ZEROCOPY,
    UseNeedWakeup = LibXdp.XDP_USE_NEED_WAKEUP
}