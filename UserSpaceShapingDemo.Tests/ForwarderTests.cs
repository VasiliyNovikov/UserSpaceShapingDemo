#pragma warning disable CS0162 // Unreachable code detected
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Forwarding;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class ForwarderTests : IForwardingLogger
{
    private const bool IsDriverZeroCopySupported = false;
    private const bool IsDisplayLogging = false;

    private static readonly TrafficForwarderType[] Types = [TrafficForwarderType.Simple, TrafficForwarderType.Parallel];
    private static readonly ForwardingMode[] Modes = IsDriverZeroCopySupported
        ? [ForwardingMode.Generic, ForwardingMode.Driver, ForwardingMode.DriverZeroCopy]
        : [ForwardingMode.Generic, ForwardingMode.Driver];
    private static readonly int[] Versions = [4, 6];

    public TestContext TestContext { get; set; } = null!;

    public static IEnumerable<object[]> Forward_Request_Reply_Arguments()
    {
        foreach (var type in Types)
        foreach (var mode in Modes)
        foreach (var version in Versions)
            yield return [type, mode, version];
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    [DynamicData(nameof(Forward_Request_Reply_Arguments))]
    public async Task Forward_Request_Reply(TrafficForwarderType type, ForwardingMode mode, int version)
    {
        const string clientMessage = "Hello from XDP client";
        const string serverMessage = "Hello back from XDP server";
        var clientMessageBytes = Encoding.ASCII.GetBytes(clientMessage);
        var serverMessageBytes = Encoding.ASCII.GetBytes(serverMessage);
        const int clientPort = 54321;
        const int serverPort = 12345;
        var anyAddress = version == 4 ? IPAddress.Any : IPAddress.IPv6Any;

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(type, mode, logger: this);

        using var client = setup.CreateSenderSocket(version, ProtocolType.Udp, clientPort);
        using var server = setup.CreateReceiverSocket(version, ProtocolType.Udp, serverPort);

        await client.SendToAsync(clientMessageBytes, new IPEndPoint(TrafficSetup.ReceiverAddress(version), serverPort), cancellationToken);

        var receivedClientMessageBytes = new byte[clientMessage.Length];
        await server.ReceiveFromAsync(receivedClientMessageBytes, new IPEndPoint(anyAddress, 0), cancellationToken);
        var receivedClientMessage = Encoding.ASCII.GetString(receivedClientMessageBytes);
        Assert.AreEqual(clientMessage, receivedClientMessage);

        await server.SendToAsync(serverMessageBytes, new IPEndPoint(TrafficSetup.SenderAddress(version), clientPort), cancellationToken);

        var receivedServerMessageBytes = new byte[serverMessage.Length];
        await client.ReceiveFromAsync(receivedServerMessageBytes, new IPEndPoint(anyAddress, 0), cancellationToken);
        var receivedServerMessage = Encoding.ASCII.GetString(receivedServerMessageBytes);
        Assert.AreEqual(serverMessage, receivedServerMessage);
    }

    public static IEnumerable<object[]> Forward_Batch_Arguments()
    {
        foreach (var type in Types)
        foreach (var mode in Modes)
        foreach (var version in Versions)
        foreach (var batchSize in new[] { 16, 64, 128 })
        {
            yield return [type, mode, batchSize, version, 1, 1];
            if (type == TrafficForwarderType.Parallel)
            {
                yield return [type, mode, batchSize, version, 2, 1];
                yield return [type, mode, batchSize, version, 2, 2];
                yield return [type, mode, batchSize, version, 4, 1];
                yield return [type, mode, batchSize, version, 4, 2];
                yield return [type, mode, batchSize, version, 4, 4];
            }
        }
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    [DynamicData(nameof(Forward_Batch_Arguments))]
    public async Task Forward_Batch(TrafficForwarderType type, ForwardingMode mode, int version, int batchSize, int rxQueues, int txQueues)
    {
        const string clientMessageTemplate = "Hello from XDP client: {0}";
        const int clientPort = 54321;
        const int serverPort = 12345;

        var clientMessages = Enumerable.Range(0, batchSize).Select(i => string.Format(CultureInfo.InvariantCulture, clientMessageTemplate, i)).ToList();

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(type, mode, rxQueueCount: (byte)rxQueues, txQueueCount: (byte)txQueues, logger: this);

        using var client = setup.CreateSenderSocket(version, ProtocolType.Udp, clientPort);
        using var server = setup.CreateReceiverSocket(version, ProtocolType.Udp, serverPort);

        var receiveTask = ReceiveBatchAsync();

        foreach (var clientMessage in clientMessages)
            await client.SendToAsync(Encoding.ASCII.GetBytes(clientMessage), new IPEndPoint(TrafficSetup.ReceiverAddress(version), serverPort), cancellationToken);

        var receivedClientMessages = await receiveTask;

        CollectionAssert.AreEquivalent(clientMessages, receivedClientMessages); // Order is not guaranteed

        return;

        async Task<List<string>> ReceiveBatchAsync()
        {
            var result = new List<string>(batchSize);
            var receivedClientMessageBytes = new byte[clientMessageTemplate.Length + 8];
            var remoteEndPoint = new IPEndPoint(version == 4 ? IPAddress.Any : IPAddress.IPv6Any, 0);
            for (var i = 0; i < batchSize; ++i)
            {
                var res = await server.ReceiveFromAsync(receivedClientMessageBytes, remoteEndPoint, cancellationToken);
                result.Add(Encoding.ASCII.GetString(receivedClientMessageBytes, 0, res.ReceivedBytes));
            }
            return result;
        }
    }

    public static IEnumerable<object[]> Forward_Stream_Arguments()
    {
        foreach (var type in Types)
        foreach (var mode in Modes)
        foreach (var version in Versions)
        foreach (var size in new[] { 1024, 1024 * 16, 1024 * 1024 })
        {
            yield return [type, mode, version, size, 1, 1];
            if (type == TrafficForwarderType.Parallel)
            {
                yield return [type, mode, version, size, 2, 1];
                yield return [type, mode, version, size, 2, 2];
                yield return [type, mode, version, size, 4, 1];
                yield return [type, mode, version, size, 4, 2];
                yield return [type, mode, version, size, 4, 4];
            }
        }
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    [DynamicData(nameof(Forward_Stream_Arguments))]
    public async Task Forward_Stream(TrafficForwarderType type, ForwardingMode mode, int version, int size, int rxQueues, int txQueues)
    {
        const int clientPort = 54321;
        const int serverPort = 12345;

        var clientMessageBytes = new byte[size];
        RandomNumberGenerator.Fill(clientMessageBytes);

        var cancellationToken = TestContext.CancellationTokenSource.Token;

        using var setup = new TrafficForwardingSetup(type, mode, rxQueueCount: (byte)rxQueues, txQueueCount: (byte)txQueues, logger: this);

        using var client = setup.CreateSenderSocket(version, ProtocolType.Tcp, clientPort);
        using var server = setup.CreateReceiverSocket(version, ProtocolType.Tcp, serverPort);

        server.Listen();

        var acceptTask = server.AcceptAsync(cancellationToken);
        await client.ConnectAsync(new IPEndPoint(TrafficSetup.ReceiverAddress(version), serverPort), cancellationToken);
        var serverSocket = await acceptTask;
        using (serverSocket)
        {
            var sendTask = client.SendAsync(clientMessageBytes, SocketFlags.None, cancellationToken);

            var receivedClientMessageBytes = new byte[size];
            var totalReceived = 0;
            while (totalReceived < size)
            {
                var received = await serverSocket.ReceiveAsync(receivedClientMessageBytes.AsMemory(totalReceived), SocketFlags.None, cancellationToken);
                totalReceived += received;
            }

            await sendTask;

            CollectionAssert.AreEqual(clientMessageBytes, receivedClientMessageBytes);
        }
    }

    private void Log(string message)
    {
        var logMessage = $"{DateTime.UtcNow:O}: {message}";
        if (IsDisplayLogging)
            TestContext.DisplayMessage(MessageLevel.Informational, logMessage);
        else
            TestContext.WriteLine(logMessage);
    }

    void IForwardingLogger.Log(string ifName, uint queueId, string message) => Log($"{ifName}:{queueId}: {message}");
    void IForwardingLogger.LogPacket(string ifName, uint queueId, string message, Span<byte> packet) => Log($"{ifName}:{queueId}: {message}\n{packet.PacketToString()}");
    void IForwardingLogger.LogError(string ifName, uint queueId, string message, Exception error) => Log($"{ifName}:{queueId}: {message}\n{error}");
    void IForwardingLogger.LogError(string message, Exception error) => Log($"{message}\n{error}");
}