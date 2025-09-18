using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

[Flags]
public enum RtnlLinkFlags : uint
{
    Up = LibNlRoute3.IFF_UP,
    Broadcast = LibNlRoute3.IFF_BROADCAST,
    Debug = LibNlRoute3.IFF_DEBUG,
    Loopback = LibNlRoute3.IFF_LOOPBACK,
    PointToPoint = LibNlRoute3.IFF_POINTOPOINT,
    NoTrailers = LibNlRoute3.IFF_NOTRAILERS,
    Running = LibNlRoute3.IFF_RUNNING,
    NoArp = LibNlRoute3.IFF_NOARP,
    Promiscuous = LibNlRoute3.IFF_PROMISC,
    AllMulticast = LibNlRoute3.IFF_ALLMULTI,
    Master = LibNlRoute3.IFF_MASTER,
    Slave = LibNlRoute3.IFF_SLAVE,
    Multicast = LibNlRoute3.IFF_MULTICAST,
    PortSel = LibNlRoute3.IFF_PORTSEL,
    AutoMedia = LibNlRoute3.IFF_AUTOMEDIA,
    Dynamic = LibNlRoute3.IFF_DYNAMIC,
    LowerUp = LibNlRoute3.IFF_LOWER_UP,
    Dormant = LibNlRoute3.IFF_DORMANT,
    Echo = LibNlRoute3.IFF_ECHO
}