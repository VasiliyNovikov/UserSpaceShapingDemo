using System;

namespace UserSpaceShapingDemo.Lib;

[Flags]
public enum XdpSocketMode : uint
{
    UpdateIfNoExist = LibBpf.XDP_FLAGS_UPDATE_IF_NOEXIST,
    Generic = LibBpf.XDP_FLAGS_SKB_MODE,
    Driver = LibBpf.XDP_FLAGS_DRV_MODE,
    Hardware = LibBpf.XDP_FLAGS_HW_MODE
}