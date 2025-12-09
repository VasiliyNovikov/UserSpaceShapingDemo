using System;
using System.Globalization;
using System.Runtime.CompilerServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Links;

public readonly struct LinkAddress<TAddress>(TAddress address, byte prefixLength)
    where TAddress : unmanaged, IIPAddress<TAddress>
{
    public TAddress Address => address;
    public byte PrefixLength => prefixLength;

    public override string ToString() => $"{Address}/{PrefixLength}";

    public static LinkAddress<TAddress> Parse(string addressString)
    {
        var slashIndex = addressString.IndexOf('/');
        if (slashIndex < 0)
        {
            var address = TAddress.Parse(addressString);
            var prefixLength = (byte)(Unsafe.SizeOf<TAddress>() * 8);
            return new(address, prefixLength);
        }
        else
        {
            var address = TAddress.Parse(addressString.AsSpan(0, slashIndex));
            var prefixLength = byte.Parse(addressString.AsSpan(slashIndex + 1), CultureInfo.InvariantCulture);
            return new(address, prefixLength);
        }
    }
}