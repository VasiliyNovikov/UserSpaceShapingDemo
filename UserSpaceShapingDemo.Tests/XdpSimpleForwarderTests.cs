using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Forwarding;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class XdpSimpleForwarderTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    [DataRow(XdpForwarderMode.Generic)]
    [DataRow(XdpForwarderMode.Driver)]
    //[DataRow(XdpForwarderMode.DriverZeroCopy)]
    public async Task XdpSocket_Forward(XdpForwarderMode mode)
    {
        const string clientMessage = "Hello from XDP client!!!";
        const string serverMessage = "Hello back from XDP server!!!";
        var clientMessageBytes = Encoding.ASCII.GetBytes(clientMessage);
        var serverMessageBytes = Encoding.ASCII.GetBytes(serverMessage);
        const int clientPort = 54321;
        const int serverPort = 12345;

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(mode, null,
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: received packet:\n{data.PacketToString()}"),
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: sent packet:\n{data.PacketToString()}"));

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
    [DataRow(XdpForwarderMode.Generic, 64)]
    [DataRow(XdpForwarderMode.Generic, 128)]
    [DataRow(XdpForwarderMode.Driver, 8)]
    [DataRow(XdpForwarderMode.Driver, 16)]
    [DataRow(XdpForwarderMode.Driver, 32)]
    [DataRow(XdpForwarderMode.Driver, 64)]
    [DataRow(XdpForwarderMode.Driver, 128)]
    public async Task XdpSocket_Forward_Batch(XdpForwarderMode mode, int batchSize)
    {
        const string clientMessageTemplate = "Hello from XDP client: {0}";
        const int clientPort = 54321;
        const int serverPort = 12345;

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(mode, null,
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: received packet:\n{data.PacketToString()}"),
            (eth, data) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {eth}: sent packet:\n{data.PacketToString()}"));

        using var client = setup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, clientPort);
        using var server = setup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, serverPort);

        var receiveTask = ReceiveBatchAsync();

        for (var i = 0; i < batchSize; ++i)
        {
            var clientMessageBytes = Encoding.ASCII.GetBytes(string.Format(CultureInfo.InvariantCulture, clientMessageTemplate, i));
            await client.SendToAsync(clientMessageBytes, new IPEndPoint(TrafficSetup.ReceiverAddress, serverPort), cancellationToken);
        }

        await receiveTask;

        return;
        async Task ReceiveBatchAsync()
        {
            var receivedClientMessageBytes = new byte[clientMessageTemplate.Length + 8];
            for (var i = 0; i < batchSize; ++i)
            {
                var res = await server.ReceiveFromAsync(receivedClientMessageBytes, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
                var clientMessage = string.Format(CultureInfo.InvariantCulture, clientMessageTemplate, i);
                var receivedClientMessage = Encoding.ASCII.GetString(receivedClientMessageBytes, 0, res.ReceivedBytes);
                Assert.AreEqual(clientMessage, receivedClientMessage);
            }
        }
    }

    
}