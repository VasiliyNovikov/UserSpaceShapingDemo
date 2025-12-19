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
                WriteIPHeader<IPv4Address, IPv4Header>(sb, ref ethernetHeader.Layer2Header<IPv4Header>(), payloadAsString);
                break;
            }
            case EthernetType.IPv6:
            {
                WriteIPHeader<IPv6Address, IPv6Header>(sb, ref ethernetHeader.Layer2Header<IPv6Header>(), payloadAsString);
                break;
            }
            case EthernetType.ARP:
            {
                WriteARPHeader(sb, ref ethernetHeader.Layer2Header<ARPHeader>());
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
          .Append("    dst_ip=").Append(ipHeader.DestinationAddress).AppendLine()
          .Append("    proto=").Append(ipHeader.Protocol).AppendLine();
        if (ipHeader.Protocol == IPProtocol.UDP)
            WriteUDPHeader(sb, ref ipHeader.Layer3Header<UDPHeader>(), payloadAsString);
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

    private static void WriteUDPHeader(StringBuilder sb, ref UDPHeader udpHeader, bool payloadAsString)
    {
        sb.AppendLine("UDP:");
        sb.Append("    src_port=").Append(udpHeader.SourcePort).AppendLine()
          .Append("    dst_port=").Append(udpHeader.DestinationPort).AppendLine();
        if (payloadAsString)
            sb.Append("    payload=").Append(Encoding.UTF8.GetString(udpHeader.Payload)).AppendLine();
    }
}