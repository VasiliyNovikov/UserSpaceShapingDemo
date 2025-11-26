using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

[Flags]
public enum XdpSocketMode : uint
{
    Default = 0,
    UpdateIfNoExist = LibXdp.XDP_FLAGS_UPDATE_IF_NOEXIST,
    Generic = LibXdp.XDP_FLAGS_SKB_MODE,
    Driver = LibXdp.XDP_FLAGS_DRV_MODE,
    Hardware = LibXdp.XDP_FLAGS_HW_MODE
}