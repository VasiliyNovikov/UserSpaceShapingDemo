#pragma warning disable IDE0063
using System;
using System.Collections.Generic;
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
    public async Task XdpSocket_Forward()
    {
        const string clientMessage = "Hello from XDP client!!!";
        const string serverMessage = "Hello from XDP server!!!";
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

            using var umem = new UMemory(8192);
            Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
            umem.GetAddresses(addresses);

            using var socket1 = new XdpSocket(umem, setup1.ReceiverName);
            using var socket2 = new XdpSocket(umem, setup2.SenderName, shared: true);
            
            var addresses1 = addresses[..(int)(umem.FrameCount / 2)];
            using (var fill = socket1.FillRing.Fill((uint)addresses1.Length))
            {
                Assert.AreEqual(umem.FillRingSize, fill.Length);
                for (var i = 0u; i < fill.Length; ++i)
                    fill[i] = addresses1[(int)i];
            }

            var addresses2 = addresses[(int)(umem.FrameCount / 2)..];
            using (var fill = socket2.FillRing.Fill((uint)addresses2.Length))
            {
                Assert.AreEqual(umem.FillRingSize, fill.Length);
                for (var i = 0u; i < fill.Length; ++i)
                    fill[i] = addresses2[(int)i];
            }

            Queue<XdpDescriptor> packetsToSend1 = [];
            Queue<XdpDescriptor> packetsToSend2 = [];
            Queue<ulong> addressesToFill1 = [];
            Queue<ulong> addressesToFill2 = [];

            using var nativeCancellationToken = new NativeCancellationToken(linkedCancellation.Token);
            
            TestContext.WriteLine($"{DateTime.UtcNow:O}: Starting forwarding loop");

            while (true)
            {
                while (ForwardOnce(socket1, socket2, packetsToSend2, addressesToFill1) |
                       ForwardOnce(socket2, socket1, packetsToSend1, addressesToFill2)) ;

                var events1 = Poll.Event.Readable;
                if (packetsToSend1.Count > 0)
                    events1 |= Poll.Event.Writable;
                var events2 = Poll.Event.Readable;
                if (packetsToSend2.Count > 0)
                    events2 |= Poll.Event.Writable;
                Assert.IsTrue(XdpSocket.WaitFor([socket1, socket2], [events1, events2], nativeCancellationToken));
            }
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

    private bool ForwardOnce(XdpSocket sourceSocket, XdpSocket destinationSocket, Queue<XdpDescriptor> packetsToSend, Queue<ulong> addressesToFill)
    {
        ulong activityCounter = 0;
        using (var receivePackets = sourceSocket.RxRing.Receive())
        {
            for (var i = 0u; i < receivePackets.Length; ++i)
            {
                ++activityCounter;
                ref readonly var packet = ref receivePackets[i];
                packetsToSend.Enqueue(packet);
                TestContext.WriteLine($"{DateTime.UtcNow:O}: Received packet ({PacketToString(sourceSocket.Umem[packet])}) from socket {sourceSocket.IfName}");
            }
        }

        using (var sendPackets = destinationSocket.TxRing.Send((uint)packetsToSend.Count))
        {
            for (var i = 0u; i < sendPackets.Length; ++i)
            {
                ++activityCounter;
                var descriptor = packetsToSend.Dequeue();
                ref var packet = ref sendPackets[i];
                packet.Address = descriptor.Address;
                packet.Length = descriptor.Length;
                var packetData = sourceSocket.Umem[descriptor];
                UpdateChecksums(packetData);
                TestContext.WriteLine($"{DateTime.UtcNow:O}: Forwarding packet ({PacketToString(packetData)}) to socket {destinationSocket.IfName}");
            }
        }

        if (destinationSocket.TxRing.NeedsWakeup)
            destinationSocket.WakeUp();

        using (var completed = destinationSocket.CompletionRing.Complete())
            for (var i = 0u; i < completed.Length; ++i)
            {
                ++activityCounter;
                addressesToFill.Enqueue(completed[i]);
                TestContext.WriteLine($"{DateTime.UtcNow:O}: Completed frame on socket {destinationSocket.IfName}");
            }

        using (var fill = sourceSocket.FillRing.Fill((uint)addressesToFill.Count))
            for (var i = 0u; i < fill.Length; ++i)
            {
                ++activityCounter;
                fill[i] = addressesToFill.Dequeue();
                TestContext.WriteLine($"{DateTime.UtcNow:O}: Refilled frame on socket {sourceSocket.IfName}");
            }

        return activityCounter > 0;
    }

    private static void UpdateChecksums(Span<byte> packetData)
    {
        ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
        if (ethernetHeader.EtherType == EthernetType.IPv4)
        {
            ref var ipv4Header = ref ethernetHeader.Layer2Header<IPv4Header>();
            if (ipv4Header.Protocol == IPProtocol.UDP)
                ipv4Header.Layer3Header<UDPHeader>().Checksum = default;
        }
    }

    private static unsafe string PacketToString(Span<byte> packetData)
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