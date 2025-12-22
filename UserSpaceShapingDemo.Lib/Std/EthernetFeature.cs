using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public enum EthernetFeature : uint
{
    TXChecksumOffload = LibC.ETHTOOL_GRXCSUM,
    RXChecksumOffload = LibC.ETHTOOL_GTXCSUM,
    ScatterGather     = LibC.ETHTOOL_GSG,
    TSO               = LibC.ETHTOOL_GTSO,
    UFO               = LibC.ETHTOOL_GUFO,
    GSO               = LibC.ETHTOOL_GGSO,
    GRO               = LibC.ETHTOOL_GGRO
}