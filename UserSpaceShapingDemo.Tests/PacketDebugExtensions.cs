using System;
using System.Runtime.CompilerServices;
using System.Text;

using UserSpaceShapingDemo.Lib.Headers;

namespace UserSpaceShapingDemo.Tests;

public static class PacketDebugExtensions
{
    public static string PacketToString(this Span<byte> packetData, bool payloadAsString = true)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Frame:");
        sb.Append("    len: ").Append(packetData.Length).AppendLine();

        ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
        sb.AppendLine("Ethernet:");
        sb.Append("    type=").Append(ethernetHeader.EtherType).AppendLine()
          .Append("    src_mac=").Append(ethernetHeader.SourceAddress).AppendLine()
          .Append("    dst_mac=").Append(ethernetHeader.DestinationAddress).AppendLine();
        switch (ethernetHeader.EtherType)
        {
            case EthernetType.IPv4:
            {
                ref var ipv4Header = ref ethernetHeader.Layer2Header<IPv4Header>();
                sb.AppendLine("IPv4:");
                sb.Append("    src_ip=").Append(ipv4Header.SourceAddress).AppendLine()
                  .Append("    dst_ip=").Append(ipv4Header.DestinationAddress).AppendLine()
                  .Append("    proto=").Append(ipv4Header.Protocol).AppendLine();
                if (ipv4Header.Protocol == IPProtocol.UDP)
                {
                    ref var udpHeader = ref ipv4Header.Layer3Header<UDPHeader>();
                    sb.AppendLine("UDP:");
                    sb.Append("    src_port=").Append(udpHeader.SourcePort).AppendLine()
                      .Append("    dst_port=").Append(udpHeader.DestinationPort).AppendLine();
                  if (payloadAsString)
                    sb.Append("    payload=").Append(Encoding.ASCII.GetString(udpHeader.Payload)).AppendLine();
                }
                break;
            }
            case EthernetType.ARP:
            {
                ref var arpHeader = ref ethernetHeader.Layer2Header<ARPHeader>();
                sb.AppendLine("ARP:");
                sb.Append("    op=").Append(arpHeader.Operation).AppendLine()
                  .Append("    src_ip=").Append(arpHeader.SenderProtocolAddress).AppendLine()
                  .Append("    src_mac=").Append(arpHeader.SenderHardwareAddress).AppendLine()
                  .Append("    dst_ip=").Append(arpHeader.TargetProtocolAddress).AppendLine()
                  .Append("    dst_mac=").Append(arpHeader.TargetHardwareAddress).AppendLine();
                break;
            }
        }
        return sb.ToString();
    }
}