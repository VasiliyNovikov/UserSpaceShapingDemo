using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IPv6PseudoHeader(ref IPv6Header header)
{
    public IPv6Address SourceAddress = header.SourceAddress;
    public IPv6Address DestinationAddress = header.DestinationAddress;
    public NetInt<uint> UpperLayerPacketLength = (NetInt<uint>)(uint)header.PayloadLength;
    public byte Zero1 = 0;
    public byte Zero2 = 0;
    public byte Zero3 = 0;
    public IPProtocol NextHeader = header.Protocol;
}