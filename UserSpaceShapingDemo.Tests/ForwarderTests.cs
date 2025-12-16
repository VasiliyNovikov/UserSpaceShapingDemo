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
public sealed class ForwarderTests : IForwardingLogger
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Timeout(2000, CooperativeCancellation = true)]
    //[DataRow(TrafficForwarderType.Simple, ForwardingMode.Generic)]
    //[DataRow(TrafficForwarderType.Simple, ForwardingMode.Driver)]
    //[DataRow(TrafficForwarderType.Simple, XdpForwarderMode.DriverZeroCopy)]
    [DataRow(TrafficForwarderType.Parallel, ForwardingMode.Generic)]
    //[DataRow(TrafficForwarderType.Parallel, ForwardingMode.Driver)]
    public async Task XdpSocket_Forward_One(TrafficForwarderType type, ForwardingMode mode)
    {
        const string clientMessage = "Hello from XDP client!!!";
        const string serverMessage = "Hello back from XDP server!!!";
        var clientMessageBytes = Encoding.ASCII.GetBytes(clientMessage);
        var serverMessageBytes = Encoding.ASCII.GetBytes(serverMessage);
        const int clientPort = 54321;
        const int serverPort = 12345;

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(type, mode, null, this);

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
    [DataRow(TrafficForwarderType.Simple, ForwardingMode.Generic, 8)]
    [DataRow(TrafficForwarderType.Simple, ForwardingMode.Generic, 32)]
    [DataRow(TrafficForwarderType.Simple, ForwardingMode.Generic, 128)]
    [DataRow(TrafficForwarderType.Simple, ForwardingMode.Driver, 8)]
    [DataRow(TrafficForwarderType.Simple, ForwardingMode.Driver, 32)]
    [DataRow(TrafficForwarderType.Simple, ForwardingMode.Driver, 128)]
    [DataRow(TrafficForwarderType.Parallel, ForwardingMode.Generic, 8)]
    [DataRow(TrafficForwarderType.Parallel, ForwardingMode.Generic, 32)]
    [DataRow(TrafficForwarderType.Parallel, ForwardingMode.Generic, 128)]
    [DataRow(TrafficForwarderType.Parallel, ForwardingMode.Driver, 8)]
    [DataRow(TrafficForwarderType.Parallel, ForwardingMode.Driver, 32)]
    [DataRow(TrafficForwarderType.Parallel, ForwardingMode.Driver, 128)]
    public async Task XdpSocket_Forward_Batch(TrafficForwarderType type, ForwardingMode mode, int batchSize)
    {
        const string clientMessageTemplate = "Hello from XDP client: {0}";
        const int clientPort = 54321;
        const int serverPort = 12345;

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(type, mode, null, this);

        using var client = setup.CreateSenderSocket(SocketType.Dgram, ProtocolType.Udp, clientPort);
        using var server = setup.CreateReceiverSocket(SocketType.Dgram, ProtocolType.Udp, serverPort);

        var receiveTask = ReceiveBatchAsync();

        for (var i = 0; i < batchSize; ++i)
        {
            var clientMessageBytes = Encoding.ASCII.GetBytes(string.Format(CultureInfo.InvariantCulture, clientMessageTemplate, i));
            await client.SendToAsync(clientMessageBytes, new IPEndPoint(TrafficSetup.ReceiverAddress, serverPort), cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);// Packets get reordered without this delay in some environments
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

    private void Log(string message) => TestContext.WriteLine($"{DateTime.UtcNow:O}: {message}");
    void IForwardingLogger.Log(string ifName, uint queueId, string message) => Log($"{ifName}:{queueId}: {message}");
    void IForwardingLogger.LogPacket(string ifName, uint queueId, string message, Span<byte> packet) => Log($"{ifName}:{queueId}: {message}\n{packet.PacketToString()}");
    void IForwardingLogger.LogError(string ifName, uint queueId, string message, Exception error) => Log($"{ifName}:{queueId}: {message}\n{error}");
    void IForwardingLogger.LogError(string message, Exception error) => Log($"{message}\n{error}");
}