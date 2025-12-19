namespace UserSpaceShapingDemo.Lib.Headers;

public enum IPProtocol : byte
{
    HOPOPT = 0,
    ICMP = 1,
    TCP = 6,
    UDP = 17,
    Route = 43,
    Fragment = 44,
    ICMPv6 = 58,
    DESTOPT = 60
}