using System;
using System.Net.Sockets;
using System.Threading;

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
        var message = "Hello from veth"u8;
        const int headerSize = 14 + 20 + 8; // Ethernet + IPv4 + UDP
        const int port = 12345;

        using var setup = new TrafficSetup();
        using var sender = setup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp);
        sender.Connect(TrafficSetup.ReceiverAddress, port);
        using (setup.EnterReceiver())
        {
            using var umem = new UMemory();
            Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
            umem.Init(addresses);
            var count = umem.FillRing.Fill(addresses[..(int)umem.FillRingSize]);
            Assert.AreEqual(umem.FillRingSize, count);

            using var socket = new XdpSocket(umem, setup.ReceiverName);

            sender.Send(message);

            using var nativeCancellationToken = new NativeCancellationToken(TestContext.CancellationTokenSource.Token);

            socket.WaitForRead(nativeCancellationToken);

            Span<XdpDescriptor> buffer = stackalloc XdpDescriptor[256];
            var receiveCount = socket.RxRing.Receive(buffer);
            Assert.AreEqual(1u, receiveCount);
            var descriptor = buffer[0];
            Assert.AreEqual((uint)message.Length + headerSize, descriptor.Length);
        }
    }
}