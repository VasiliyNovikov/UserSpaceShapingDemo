using System.Net.Sockets;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Links;

public readonly struct LinkAddress<TAddress>(TAddress address, byte prefixLength)
    where TAddress : unmanaged, IIPAddress<TAddress>
{
    public TAddress Address => address;
    public byte PrefixLength => prefixLength;
}