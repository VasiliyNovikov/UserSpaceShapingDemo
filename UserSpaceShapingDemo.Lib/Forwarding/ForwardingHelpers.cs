using System;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Headers;

namespace UserSpaceShapingDemo.Lib.Forwarding;

internal static class ForwardingHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateChecksums(Span<byte> packetData)
    {
        ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
        if (ethernetHeader.EtherType == EthernetType.IPv4)
        {
            ref var ipv4Header = ref ethernetHeader.NextHeader<IPv4Header>();
            if (ipv4Header.Protocol == IPProtocol.UDP)
                ipv4Header.NextHeader<UDPHeader>().Checksum = default;
        }
    }
}