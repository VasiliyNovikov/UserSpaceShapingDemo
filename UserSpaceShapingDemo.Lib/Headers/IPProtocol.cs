namespace UserSpaceShapingDemo.Lib.Headers;

public enum IPProtocol : byte
{
    IPv6HopOpts = 0,
    ICMP = 1,
    TCP = 6,
    UDP = 17,
    IPv6Route = 43,
    IPv6Fragment = 44,
    ICMPv6 = 58,
    IPv6DestOpts = 60
}