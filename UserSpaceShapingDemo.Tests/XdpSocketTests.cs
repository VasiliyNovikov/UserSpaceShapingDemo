#pragma warning disable IDE0063
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed unsafe class XdpSocketTests
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
    [Timeout(10000, CooperativeCancellation = true)]
    public void XdpSocket_Receive()
    {
        const string message = "Hello from XDP client!!!";
        const string replyMessage = "Hello from XDP server!!!";
        var messageBytes = Encoding.ASCII.GetBytes(message);
        var replyMessageBytes = Encoding.ASCII.GetBytes(replyMessage);
        const int senderPort = 54321;
        const int receiverPort = 12345;
        const byte ipv4HeaderVersionAndLength = 0b01000101; // Version 4, Header Length 5

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
            umem.Init(addresses);
            var count = umem.FillRing.Fill(addresses[..(int)umem.FillRingSize]);
            Assert.AreEqual(umem.FillRingSize, count);

            using var socket = new XdpSocket(umem, setup.ReceiverName);

            sender.Send(messageBytes);

            using var nativeCancellationToken = new NativeCancellationToken(TestContext.CancellationTokenSource.Token);

            socket.WaitForRead(nativeCancellationToken);

            using (var receivePackets = socket.RxRing.Receive(256))
            {
                Assert.AreEqual(1u, receivePackets.Length);
                ref readonly var packet = ref receivePackets[0];

                var packetData = umem[packet];

                ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
                Assert.AreEqual(0x0800, (ushort)ethernetHeader.EtherType); // IPv4
                Assert.AreEqual(TrafficSetup.SenderMacAddress, ethernetHeader.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverMacAddress, ethernetHeader.DestinationAddress);

                ref var ipv4Header = ref Unsafe.As<byte, IPv4Header>(ref packetData[sizeof(EthernetHeader)]);
                Assert.AreEqual(ipv4HeaderVersionAndLength, ipv4Header.VersionAndHeaderLength);
                Assert.AreEqual(17, ipv4Header.Protocol); // UDP
                Assert.AreEqual(TrafficSetup.SenderAddress, ipv4Header.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverAddress, ipv4Header.DestinationAddress);

                ref var udpHeader = ref Unsafe.As<byte, UDPHeader>(ref packetData[sizeof(EthernetHeader) + sizeof(IPv4Header)]);
                Assert.AreEqual(receiverPort, (ushort)udpHeader.DestinationPort);
                Assert.AreEqual(senderPort, (ushort)udpHeader.SourcePort);

                var payload = packetData[(sizeof(EthernetHeader) + sizeof(IPv4Header) + sizeof(UDPHeader))..];
                Assert.AreEqual(message.Length, payload.Length);
                var payloadString = Encoding.ASCII.GetString(payload);
                Assert.AreEqual(message, payloadString);
            }

            using (var sendPackets = socket.TxRing.Send(1))
            {
                Assert.AreEqual(1u, sendPackets.Length);

                ref var packet = ref sendPackets[0];
                packet.Length = (uint)(sizeof(EthernetHeader) + sizeof(IPv4Header) + sizeof(UDPHeader) + replyMessageBytes.Length);
                var packetData = umem[packet];

                ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
                ethernetHeader.DestinationAddress = TrafficSetup.SenderMacAddress;
                ethernetHeader.SourceAddress = TrafficSetup.ReceiverMacAddress;
                ethernetHeader.EtherType = (NetInt<ushort>)0x0800; // IPv4

                ref var ipv4Header = ref Unsafe.As<byte, IPv4Header>(ref packetData[sizeof(EthernetHeader)]);
                ipv4Header.VersionAndHeaderLength = ipv4HeaderVersionAndLength;
                ipv4Header.TypeOfService = 0;
                ipv4Header.TotalLength = (NetInt<ushort>)(sizeof(IPv4Header) + sizeof(UDPHeader) + replyMessageBytes.Length);
                ipv4Header.Id = default;
                ipv4Header.FragmentOffset = default;
                ipv4Header.Ttl = 64;
                ipv4Header.Protocol = 17; // UDP
                ipv4Header.Checksum = default; // Let the OS calculate the checksum
                ipv4Header.SourceAddress = TrafficSetup.ReceiverAddress;
                ipv4Header.DestinationAddress = TrafficSetup.SenderAddress;

                ref var udpHeader = ref Unsafe.As<byte, UDPHeader>(ref packetData[sizeof(EthernetHeader) + sizeof(IPv4Header)]);
                udpHeader.SourcePort = (NetInt<ushort>)receiverPort;
                udpHeader.DestinationPort = (NetInt<ushort>)senderPort;
                udpHeader.Size = (NetInt<ushort>)(sizeof(UDPHeader) + replyMessageBytes.Length);
                udpHeader.Checksum = default; // Let the OS calculate the checksum

                var payload = packetData[(sizeof(EthernetHeader) + sizeof(IPv4Header) + sizeof(UDPHeader))..];
                replyMessageBytes.CopyTo(payload);
            }

            if (socket.TxRing.NeedsWakeup)
            {
                // Send empty packet to wake up the TX processing
            }

            {
                Span<byte> receiveMessageBytes = stackalloc byte[replyMessage.Length];
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                sender.ReceiveFrom(receiveMessageBytes, ref endPoint);
                var receiveMessage = Encoding.ASCII.GetString(receiveMessageBytes);
                Assert.AreEqual(replyMessage, receiveMessage);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EthernetHeader
    {
        public MACAddress DestinationAddress;
        public MACAddress SourceAddress;
        public NetInt<ushort> EtherType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IPv4Header
    {
        public byte VersionAndHeaderLength;
        public byte TypeOfService;
        public NetInt<ushort> TotalLength;
        public NetInt<ushort> Id;
        public NetInt<ushort> FragmentOffset;
        public byte Ttl;
        public byte Protocol;
        public NetInt<ushort> Checksum;
        public IPv4Address SourceAddress;
        public IPv4Address DestinationAddress;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct UDPHeader
    {
        public NetInt<ushort> SourcePort;
        public NetInt<ushort> DestinationPort;
        public NetInt<ushort> Size;
        public NetInt<ushort> Checksum;
    }
}