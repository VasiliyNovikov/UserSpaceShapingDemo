using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

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
        using var sender = setup.CreateSenderSocket(4, ProtocolType.Udp, senderPort);
        sender.Connect(TrafficSetup.ReceiverAddress(4), receiverPort);
        using (var receiver = setup.CreateReceiverSocket(4, ProtocolType.Udp, receiverPort))
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

            var fill = socket.FillRing.Fill(umem.FillRingSize);
            Assert.AreEqual(umem.FillRingSize, fill.Length);
            for (var i = 0u; i < fill.Length; ++i)
                fill[i] = addresses[(int)i];
            fill.Submit();

            sender.Send(messageBytes);

            using var nativeCancellationToken = new NativeCancellationToken(TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(socket.WaitFor(Poll.Event.Readable, nativeCancellationToken));

            ulong receivedAddress;

            var receivePackets = socket.RxRing.Receive();
            {
                Assert.AreEqual(1u, receivePackets.Length);
                var packet = receivePackets[0];
                receivePackets.Release();

                receivedAddress = packet.Address;

                var packetData = umem[packet];

                ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
                Assert.AreEqual(EthernetType.IPv4, ethernetHeader.EtherType);
                Assert.AreEqual(TrafficSetup.SenderMacAddress, ethernetHeader.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverMacAddress, ethernetHeader.DestinationAddress);

                ref var ipv4Header = ref ethernetHeader.NextHeader<IPv4Header>();
                Assert.AreEqual(4, ipv4Header.Version);
                Assert.AreEqual(sizeof(IPv4Header), ipv4Header.HeaderLength);
                Assert.AreEqual(IPProtocol.UDP, ipv4Header.Protocol);
                Assert.AreEqual(TrafficSetup.SenderAddress(4), ipv4Header.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverAddress(4), ipv4Header.DestinationAddress);

                ref var udpHeader = ref ipv4Header.NextHeader<UDPHeader>();
                Assert.AreEqual(receiverPort, udpHeader.DestinationPort);
                Assert.AreEqual(senderPort, udpHeader.SourcePort);

                var payload = udpHeader.Payload;
                Assert.AreEqual(message.Length, payload.Length);
                var payloadString = Encoding.ASCII.GetString(payload);
                Assert.AreEqual(message, payloadString);
            }

            var sendPackets = socket.TxRing.Send(1);
            {
                Assert.AreEqual(1u, sendPackets.Length);

                ref var packet = ref sendPackets[0];
                packet.Address = receivedAddress;
                packet.Length = (uint)(sizeof(EthernetHeader) + sizeof(IPv4Header) + sizeof(UDPHeader) +
                                       replyMessageBytes.Length);
                var packetData = umem[packet];

                ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
                ethernetHeader.DestinationAddress = TrafficSetup.SenderMacAddress;
                ethernetHeader.SourceAddress = TrafficSetup.ReceiverMacAddress;
                ethernetHeader.EtherType = EthernetType.IPv4;

                ref var ipv4Header = ref ethernetHeader.NextHeader<IPv4Header>();
                ipv4Header.Version = 4;
                ipv4Header.HeaderLength = (byte)sizeof(IPv4Header);
                ipv4Header.TrafficClass = 0;
                ipv4Header.TotalLength = (ushort)(sizeof(IPv4Header) + sizeof(UDPHeader) + replyMessageBytes.Length);
                ipv4Header.Identification = 0;
                ipv4Header.FragmentOffset = 0;
                ipv4Header.Ttl = 64;
                ipv4Header.Protocol = IPProtocol.UDP;
                ipv4Header.SourceAddress = TrafficSetup.ReceiverAddress(4);
                ipv4Header.DestinationAddress = TrafficSetup.SenderAddress(4);
                ipv4Header.UpdateChecksum();

                ref var udpHeader = ref ipv4Header.NextHeader<UDPHeader>();
                udpHeader.SourcePort = receiverPort;
                udpHeader.DestinationPort = senderPort;
                udpHeader.Size = (ushort)(sizeof(UDPHeader) + replyMessageBytes.Length);
                udpHeader.UpdateChecksum();

                replyMessageBytes.CopyTo(udpHeader.Payload);
            }
            sendPackets.Submit();

            if (socket.TxRing.NeedsWakeup)
                socket.WakeUp();

            {
                Span<byte> receiveMessageBytes = stackalloc byte[replyMessage.Length];
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                sender.ReceiveFrom(receiveMessageBytes, ref endPoint);
                var receiveMessage = Encoding.ASCII.GetString(receiveMessageBytes);
                Assert.AreEqual(replyMessage, receiveMessage);
            }

            var completed = socket.CompletionRing.Complete();
            Assert.AreEqual(1u, completed.Length);
            fill = socket.FillRing.Fill(1);
            Assert.AreEqual(1u, fill.Length);
            fill[0] = completed[0];
            fill.Submit();
            completed.Release();
        }
    }
}