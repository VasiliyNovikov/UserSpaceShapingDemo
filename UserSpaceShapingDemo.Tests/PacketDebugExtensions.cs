using System;
using System.Runtime.CompilerServices;
using System.Text;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib.Headers;

namespace UserSpaceShapingDemo.Tests;

public static class PacketDebugExtensions
{
    public static string PacketToString(this Span<byte> packetData, bool payloadAsString = true)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Frame:");
        sb.Append("    len=").Append(packetData.Length).AppendLine();

        ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
        sb.AppendLine("Ethernet:");
        sb.Append("    type=").Append(ethernetHeader.EtherType).AppendLine()
          .Append("    src_mac=").Append(ethernetHeader.SourceAddress).AppendLine()
          .Append("    dst_mac=").Append(ethernetHeader.DestinationAddress).AppendLine();
        switch (ethernetHeader.EtherType)
        {
            case EthernetType.IPv4:
            {
                WriteIPHeader<IPv4Address, IPv4Header>(sb, ref ethernetHeader.NextHeader<IPv4Header>(), payloadAsString);
                break;
            }
            case EthernetType.IPv6:
            {
                WriteIPHeader<IPv6Address, IPv6Header>(sb, ref ethernetHeader.NextHeader<IPv6Header>(), payloadAsString);
                break;
            }
            case EthernetType.ARP:
            {
                WriteARPHeader(sb, ref ethernetHeader.NextHeader<ARPHeader>());
                break;
            }
        }
        return sb.ToString();
    }

    private static void WriteIPHeader<TAddress, TIPHeader>(StringBuilder sb, ref TIPHeader ipHeader, bool payloadAsString)
        where TAddress : unmanaged, IIPAddress<TAddress>
        where TIPHeader : IIPHeader<TAddress>
    {
        sb.Append("IPv").Append(ipHeader.Version).AppendLine(":");
        sb.Append("    src_ip=").Append(ipHeader.SourceAddress).AppendLine()
          .Append("    dst_ip=").Append(ipHeader.DestinationAddress).AppendLine();
        WriteNextHeader(sb, ref ipHeader, payloadAsString);
    }

    private static void WriteARPHeader(StringBuilder sb, ref ARPHeader arpHeader)
    {
        sb.AppendLine("ARP:");
        sb.Append("    op=").Append(arpHeader.Operation).AppendLine()
          .Append("    src_ip=").Append(arpHeader.SenderProtocolAddress).AppendLine()
          .Append("    src_mac=").Append(arpHeader.SenderHardwareAddress).AppendLine()
          .Append("    dst_ip=").Append(arpHeader.TargetProtocolAddress).AppendLine()
          .Append("    dst_mac=").Append(arpHeader.TargetHardwareAddress).AppendLine();
    }

    private static void WriteNextHeader<THeader>(StringBuilder sb, ref THeader header, bool payloadAsString)
        where THeader : IHasNextHeader
    {
        sb.Append("    proto=").Append(header.Protocol).AppendLine();
        switch (header.Protocol)
        {
            case IPProtocol.UDP:
                WriteUDPHeader(sb, ref header.NextHeader<UDPHeader>(), payloadAsString);
                break;
            case IPProtocol.IPv6HopOpts:
            case IPProtocol.IPv6Route:
            case IPProtocol.IPv6Fragment:
            case IPProtocol.IPv6DestOpts:
                WriteIP6ExtensionHeader(sb, header.Protocol, ref header.NextHeader<IPv6ExtensionHeader>(), payloadAsString);
                break;
        }
    }

    private static void WriteIP6ExtensionHeader(StringBuilder sb, IPProtocol type, ref IPv6ExtensionHeader extHeader, bool payloadAsString)
    {
        sb.Append(type).AppendLine(":");
        sb.Append("    len=").Append(extHeader.HeaderLength).AppendLine();
        WriteNextHeader(sb, ref extHeader, payloadAsString);
    }

    private static void WriteUDPHeader(StringBuilder sb, ref UDPHeader udpHeader, bool payloadAsString)
    {
        sb.AppendLine("UDP:");
        sb.Append("    src_port=").Append(udpHeader.SourcePort).AppendLine()
          .Append("    dst_port=").Append(udpHeader.DestinationPort).AppendLine();
        if (payloadAsString)
            sb.Append("    payload=").Append(Encoding.UTF8.GetString(udpHeader.Payload)).AppendLine();
    }
}