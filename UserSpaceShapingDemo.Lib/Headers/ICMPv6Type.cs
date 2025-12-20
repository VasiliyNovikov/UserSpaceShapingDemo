namespace UserSpaceShapingDemo.Lib.Headers;

public enum ICMPv6Type : byte
{
    DestinationUnreachable          =   1,
    PacketTooBig                    =   2,
    TimeExceeded                    =   3,
    ParameterProblem                =   4,
    EchoRequest                     = 128,
    EchoReply                       = 129,
    RouterSolicitation              = 133,
    RouterAdvertisement             = 134,
    NeighborSolicitation            = 135,
    NeighborAdvertisement           = 136,
    RedirectMessage                 = 137,
    RouterRenumbering               = 138,
    Version2MulticastListenerReport = 143
}