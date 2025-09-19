using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class TrafficSetupTests
{
    [TestMethod]
    public void TrafficSetup_Create_Destroy()
    {
        using var trafficSetup = new TrafficSetup();
        Assert.IsNotNull(trafficSetup);
    }

    [TestMethod]
    public void TrafficSetup_Ping()
    {
        var message = "Hello from veth"u8.ToArray();
        using var trafficSetup = new TrafficSetup();
        using (trafficSetup.EnterSender())
        {
            using var ping = new Ping();
            var reply = ping.Send(TrafficSetup.ReceiverAddress, 500, message);
            Assert.IsNotNull(reply);
            Assert.AreEqual(IPStatus.Success, reply.Status);
            CollectionAssert.AreEqual(message, reply.Buffer);
        }
    }

    [TestMethod]
    public void TrafficSetup_Sockets()
    {
        var message = "Hello from veth"u8;
        const int port = 12345;

        using var trafficSetup = new TrafficSetup();
        using var sender = trafficSetup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp);
        using var receiver = trafficSetup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, port);
        sender.Connect(TrafficSetup.ReceiverAddress, port);

        var bytesSent = sender.Send(message);

        Assert.AreEqual(message.Length, bytesSent);

        Span<byte> receivedMessage = stackalloc byte[message.Length];
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        var bytesReceived = receiver.ReceiveFrom(receivedMessage, ref endPoint);

        Assert.IsInstanceOfType<IPEndPoint>(endPoint, out var senderEndPoint);
        Assert.AreEqual(TrafficSetup.SenderAddress, senderEndPoint.Address);
        Assert.AreEqual(message.Length, bytesReceived);
        Assert.IsTrue(message.SequenceEqual(receivedMessage));
    }
}