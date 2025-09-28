using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib;
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
    [Timeout(10000, CooperativeCancellation = true)]
    public void XdpSocket_Receive()
    {
        const string message = "Hello from XDP!!!";
        var messageBytes = Encoding.ASCII.GetBytes(message);
        const int headerSize = 14 + 20 + 8; // Ethernet + IPv4 + UDP
        const int port = 12345;

        using var setup = new TrafficSetup();
        using var sender = setup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp);
        sender.Connect(TrafficSetup.ReceiverAddress, port);
        using (var receiver = setup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, port))
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

            using var packets = socket.RxRing.Receive(256);
            Assert.AreEqual(1u, packets.Length);
            ref readonly var packet = ref packets[0];
            var payloadLength = packet.Length - headerSize;
            Assert.AreEqual((uint)message.Length, payloadLength);

            var packetData = umem[packet];

            ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
            Assert.AreEqual(0x0800, IPAddress.NetworkToHostOrder((short)ethernetHeader.EtherType)); // IPv4

            var payload = packetData[headerSize..];
            var payloadString = Encoding.ASCII.GetString(payload);
            Assert.AreEqual(message, payloadString);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private unsafe struct EthernetHeader
    {
        public fixed byte DestinationAddress[6];
        public fixed byte SourceAddress[6];
        public readonly ushort EtherType;
    }
}