using System.Net.Sockets;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Links;

public readonly struct LinkAddress<TAddress>(TAddress address, byte prefixLength)
    where TAddress : unmanaged, IIPAddress<TAddress>
{
    public static AddressFamily Family => TAddress.Version == 4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;

    public TAddress Address => address;
    public byte PrefixLength => prefixLength;
}