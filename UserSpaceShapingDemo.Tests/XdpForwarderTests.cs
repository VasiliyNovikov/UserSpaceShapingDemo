using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Lib.Headers;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class XdpForwarderTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    [DataRow(XdpForwarderMode.Generic)]
    [DataRow(XdpForwarderMode.Driver)]
    [DataRow(XdpForwarderMode.DriverZeroCopy)]
    public async Task XdpSocket_Forward(XdpForwarderMode mode)
    {
        const string clientMessage = "Hello from XDP client!!!";
        const string serverMessage = "Hello back from XDP server!!!";
        var clientMessageBytes = Encoding.ASCII.GetBytes(clientMessage);
        var serverMessageBytes = Encoding.ASCII.GetBytes(serverMessage);
        const int clientPort = 54321;
        const int serverPort = 12345;

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(mode,
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: received packet {PacketToString(data)}"),
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: sent packet {PacketToString(data)}"));

        using var client = setup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, clientPort);
        using var server = setup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, serverPort);

        await client.SendToAsync(clientMessageBytes, new IPEndPoint(TrafficSetup.ReceiverAddress, serverPort), cancellationToken);

        var receivedClientMessageBytes = new byte[clientMessage.Length];
        await server.ReceiveFromAsync(receivedClientMessageBytes, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
        var receivedClientMessage = Encoding.ASCII.GetString(receivedClientMessageBytes);
        Assert.AreEqual(clientMessage, receivedClientMessage);

        await server.SendToAsync(serverMessageBytes, new IPEndPoint(TrafficSetup.SenderAddress, clientPort), cancellationToken);

        var receivedServerMessageBytes = new byte[serverMessage.Length];
        await client.ReceiveFromAsync(receivedServerMessageBytes, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
        var receivedServerMessage = Encoding.ASCII.GetString(receivedServerMessageBytes);
        Assert.AreEqual(serverMessage, receivedServerMessage);
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    [DataRow(XdpForwarderMode.Generic, 8)]
    [DataRow(XdpForwarderMode.Generic, 16)]
    [DataRow(XdpForwarderMode.Generic, 32)]
    [DataRow(XdpForwarderMode.Driver, 8)]
    [DataRow(XdpForwarderMode.Driver, 16)]
    [DataRow(XdpForwarderMode.Driver, 32)]
    public async Task XdpSocket_Forward_Batch(XdpForwarderMode mode, int batchSize)
    {
        const string clientMessage = "Hello from XDP client!!!";
        var clientMessageBytes = Encoding.ASCII.GetBytes(clientMessage);
        const int clientPort = 54321;
        const int serverPort = 12345;

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(mode,
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: received packet {PacketToString(data)}"),
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: sent packet {PacketToString(data)}"));

        using var client = setup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, clientPort);
        using var server = setup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, serverPort);

        var receiveTask = ReceiveBatchAsync();

        for (var i = 0; i < batchSize; ++i)
            await client.SendToAsync(clientMessageBytes, new IPEndPoint(TrafficSetup.ReceiverAddress, serverPort), cancellationToken);

        await receiveTask;

        return;
        async Task ReceiveBatchAsync()
        {
            var receivedClientMessageBytes = new byte[clientMessage.Length];
            for (var i = 0; i < batchSize; ++i)
            {
                await server.ReceiveFromAsync(receivedClientMessageBytes, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
                var receivedClientMessage = Encoding.ASCII.GetString(receivedClientMessageBytes);
                Assert.AreEqual(clientMessage, receivedClientMessage);
            }
        }
    }

    private static string PacketToString(Span<byte> packetData)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"len: {packetData.Length}");
        ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
        sb.Append(CultureInfo.InvariantCulture, $", type={ethernetHeader.EtherType}, src_mac={ethernetHeader.SourceAddress}, dst_mac={ethernetHeader.DestinationAddress}");
        switch (ethernetHeader.EtherType)
        {
            case EthernetType.IPv4:
            {
                ref var ipv4Header = ref ethernetHeader.Layer2Header<IPv4Header>();
                sb.Append(CultureInfo.InvariantCulture, $", src_ip={ipv4Header.SourceAddress}, dst_ip={ipv4Header.DestinationAddress}, proto={ipv4Header.Protocol}");
                if (ipv4Header.Protocol == IPProtocol.UDP)
                {
                    ref var udpHeader = ref ipv4Header.Layer3Header<UDPHeader>();
                    sb.Append(CultureInfo.InvariantCulture, $", src_port={udpHeader.SourcePort}, dst_port={udpHeader.DestinationPort}");
                }
                break;
            }
            case EthernetType.ARP:
            {
                ref var arpHeader = ref ethernetHeader.Layer2Header<ARPHeader>();
                sb.Append(CultureInfo.InvariantCulture, $", op={arpHeader.Operation}, src_ip={arpHeader.SenderProtocolAddress}, src_mac={arpHeader.SenderHardwareAddress}, dst_ip={arpHeader.TargetProtocolAddress}, dst_mac={arpHeader.TargetHardwareAddress}");
                break;
            }
        }
        return sb.ToString();
    }
}