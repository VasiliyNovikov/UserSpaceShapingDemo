#pragma warning disable IDE0063
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Lib.Headers;
using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class XdpSocketTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void XdpSocket_Open_Close()
    {
        using var setup = new TrafficSetup();
        using (setup.EnterReceiver())
        {
            using var umem = new UMemory();
            using var socket = new XdpSocket(umem, setup.ReceiverName);
            Assert.IsNotNull(socket);
        }
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    public unsafe void XdpSocket_Receive_Send()
    {
        const string message = "Hello from XDP client!!!";
        const string replyMessage = "Hello from XDP server!!!";
        var messageBytes = Encoding.ASCII.GetBytes(message);
        var replyMessageBytes = Encoding.ASCII.GetBytes(replyMessage);
        const int senderPort = 54321;
        const int receiverPort = 12345;

        using var setup = new TrafficSetup();
        using var sender = setup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, senderPort);
        sender.Connect(TrafficSetup.ReceiverAddress, receiverPort);
        using (var receiver = setup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, receiverPort))
        {
            sender.Send(messageBytes);
            Span<byte> receivedMessageBytes = stackalloc byte[message.Length];
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            receiver.ReceiveFrom(receivedMessageBytes, ref endPoint);
            var receivedMessage = Encoding.ASCII.GetString(receivedMessageBytes);
            Assert.AreEqual(message, receivedMessage);
        }
        // After this block ARP resolution should be done and XDP should be able to capture actual packets.
        using (setup.EnterReceiver())
        {
            using var umem = new UMemory();
            Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
            umem.GetAddresses(addresses);

            using var socket = new XdpSocket(umem, setup.ReceiverName);

            using (var fill = socket.FillRing.Fill(umem.FillRingSize))
            {
                Assert.AreEqual(umem.FillRingSize, fill.Length);
                for (var i = 0u; i < fill.Length; ++i)
                    fill[i] = addresses[(int)i];
            }

            sender.Send(messageBytes);

            using var nativeCancellationToken = new NativeCancellationToken(TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(socket.WaitFor(Poll.Event.Readable, nativeCancellationToken));

            ulong receivedAddress;

            using (var receivePackets = socket.RxRing.Receive())
            {
                Assert.AreEqual(1u, receivePackets.Length);
                ref readonly var packet = ref receivePackets[0];
                receivedAddress = packet.Address;

                var packetData = umem[packet];

                ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
                Assert.AreEqual(EthernetType.IPv4, ethernetHeader.EtherType);
                Assert.AreEqual(TrafficSetup.SenderMacAddress, ethernetHeader.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverMacAddress, ethernetHeader.DestinationAddress);

                ref var ipv4Header = ref ethernetHeader.Layer2Header<IPv4Header>();
                Assert.AreEqual(4, ipv4Header.Version);
                Assert.AreEqual(sizeof(IPv4Header), ipv4Header.HeaderLength);
                Assert.AreEqual(IPProtocol.UDP, ipv4Header.Protocol);
                Assert.AreEqual(TrafficSetup.SenderAddress, ipv4Header.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverAddress, ipv4Header.DestinationAddress);

                ref var udpHeader = ref ipv4Header.Layer3Header<UDPHeader>();
                Assert.AreEqual(receiverPort, udpHeader.DestinationPort);
                Assert.AreEqual(senderPort, udpHeader.SourcePort);

                var payload = udpHeader.Payload;
                Assert.AreEqual(message.Length, payload.Length);
                var payloadString = Encoding.ASCII.GetString(payload);
                Assert.AreEqual(message, payloadString);
            }

            using (var sendPackets = socket.TxRing.Send(1))
            {
                Assert.AreEqual(1u, sendPackets.Length);

                ref var packet = ref sendPackets[0];
                packet.Address = receivedAddress;
                packet.Length = (uint)(sizeof(EthernetHeader) + sizeof(IPv4Header) + sizeof(UDPHeader) + replyMessageBytes.Length);
                var packetData = umem[packet];

                ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
                ethernetHeader.DestinationAddress = TrafficSetup.SenderMacAddress;
                ethernetHeader.SourceAddress = TrafficSetup.ReceiverMacAddress;
                ethernetHeader.EtherType = EthernetType.IPv4;

                ref var ipv4Header = ref ethernetHeader.Layer2Header<IPv4Header>();
                ipv4Header.Version = 4;
                ipv4Header.HeaderLength = (byte)sizeof(IPv4Header);
                ipv4Header.TypeOfService = 0;
                ipv4Header.TotalLength = (ushort)(sizeof(IPv4Header) + sizeof(UDPHeader) + replyMessageBytes.Length);
                ipv4Header.Id = default;
                ipv4Header.FragmentOffset = default;
                ipv4Header.Ttl = 64;
                ipv4Header.Protocol = IPProtocol.UDP;
                ipv4Header.SourceAddress = TrafficSetup.ReceiverAddress;
                ipv4Header.DestinationAddress = TrafficSetup.SenderAddress;
                ipv4Header.UpdateChecksum();

                ref var udpHeader = ref ipv4Header.Layer3Header<UDPHeader>();
                udpHeader.SourcePort = receiverPort;
                udpHeader.DestinationPort = senderPort;
                udpHeader.Size = (ushort)(sizeof(UDPHeader) + replyMessageBytes.Length);
                udpHeader.Checksum = default;

                replyMessageBytes.CopyTo(udpHeader.Payload);
            }

            if (socket.TxRing.NeedsWakeup)
                socket.WakeUp();

            {
                Span<byte> receiveMessageBytes = stackalloc byte[replyMessage.Length];
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                sender.ReceiveFrom(receiveMessageBytes, ref endPoint);
                var receiveMessage = Encoding.ASCII.GetString(receiveMessageBytes);
                Assert.AreEqual(replyMessage, receiveMessage);
            }

            using (var completed = socket.CompletionRing.Complete())
            {
                Assert.AreEqual(1u, completed.Length);
                using var fill = socket.FillRing.Fill(1);
                Assert.AreEqual(1u, fill.Length);
                fill[0] = completed[0];
            }
        }
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    [DataRow(XdpForwarderMode.GenericSharedMemory)]
    [DataRow(XdpForwarderMode.GenericCopy)]
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

        using var setup1 = new TrafficSetup();
        using var setup2 = new TrafficSetup(sharedSenderNs: setup1.ReceiverNs);

        using var forwardCancellation = new CancellationTokenSource();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(forwardCancellation.Token, cancellationToken);

        var forwarderTask = Task.Factory.StartNew(() =>
        {
            using var forwardNs = setup1.EnterReceiver();
            TestContext.WriteLine($"{DateTime.UtcNow:O}: Starting forwarding loop");
            XdpForwarder.Run(setup1.ReceiverName, setup2.SenderName, mode,
                             (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: received packet {PacketToString(data)}"),
                             (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: sent packet {PacketToString(data)}"),
                             linkedCancellation.Token);
        }, TaskCreationOptions.LongRunning);

        if (forwarderTask.IsCompleted)
            await forwarderTask;

        using var client = setup1.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, clientPort);
        using var server = setup2.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, serverPort);

        await client.SendToAsync(clientMessageBytes, new IPEndPoint(TrafficSetup.ReceiverAddress, serverPort), cancellationToken);

        if (forwarderTask.IsCompleted)
            await forwarderTask;

        var receivedClientMessageBytes = new byte[clientMessage.Length];
        await server.ReceiveFromAsync(receivedClientMessageBytes, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
        var receivedClientMessage = Encoding.ASCII.GetString(receivedClientMessageBytes);
        Assert.AreEqual(clientMessage, receivedClientMessage);

        await server.SendToAsync(serverMessageBytes, new IPEndPoint(TrafficSetup.SenderAddress, clientPort), cancellationToken);

        if (forwarderTask.IsCompleted)
            await forwarderTask;

        var receivedServerMessageBytes = new byte[serverMessage.Length];
        await client.ReceiveFromAsync(receivedServerMessageBytes, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
        var receivedServerMessage = Encoding.ASCII.GetString(receivedServerMessageBytes);
        Assert.AreEqual(serverMessage, receivedServerMessage);

        if (forwarderTask.IsCompleted)
            await forwarderTask;

        await forwardCancellation.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => forwarderTask);
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