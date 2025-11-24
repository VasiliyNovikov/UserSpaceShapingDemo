#pragma warning disable IDE0063
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Lib.Std;
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
    public void XdpSocket_Receive_Send()
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

            using (var receivePackets = socket.RxRing.Receive(256))
            {
                Assert.AreEqual(1u, receivePackets.Length);
                ref readonly var packet = ref receivePackets[0];
                receivedAddress = packet.Address;

                var packetData = umem[packet];

                ref var ethernetHeader = ref Unsafe.As<byte, EthernetHeader>(ref packetData[0]);
                Assert.AreEqual(EthernetType.IPv4, ethernetHeader.EtherType);
                Assert.AreEqual(TrafficSetup.SenderMacAddress, ethernetHeader.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverMacAddress, ethernetHeader.DestinationAddress);

                ref var ipv4Header = ref Unsafe.As<byte, IPv4Header>(ref packetData[sizeof(EthernetHeader)]);
                Assert.AreEqual(ipv4HeaderVersionAndLength, ipv4Header.VersionAndHeaderLength);
                Assert.AreEqual(IPProtocol.UDP, ipv4Header.Protocol);
                Assert.AreEqual(TrafficSetup.SenderAddress, ipv4Header.SourceAddress);
                Assert.AreEqual(TrafficSetup.ReceiverAddress, ipv4Header.DestinationAddress);

                ref var udpHeader = ref Unsafe.As<byte, UDPHeader>(ref packetData[sizeof(EthernetHeader) + sizeof(IPv4Header)]);
                Assert.AreEqual(receiverPort, udpHeader.DestinationPort);
                Assert.AreEqual(senderPort, udpHeader.SourcePort);

                var payload = packetData[(sizeof(EthernetHeader) + sizeof(IPv4Header) + sizeof(UDPHeader))..];
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

                ref var ipv4Header = ref Unsafe.As<byte, IPv4Header>(ref packetData[sizeof(EthernetHeader)]);
                ipv4Header.VersionAndHeaderLength = ipv4HeaderVersionAndLength;
                ipv4Header.TypeOfService = 0;
                ipv4Header.TotalLength = (ushort)(sizeof(IPv4Header) + sizeof(UDPHeader) + replyMessageBytes.Length);
                ipv4Header.Id = default;
                ipv4Header.FragmentOffset = default;
                ipv4Header.Ttl = 64;
                ipv4Header.Protocol = IPProtocol.UDP;
                ipv4Header.SourceAddress = TrafficSetup.ReceiverAddress;
                ipv4Header.DestinationAddress = TrafficSetup.SenderAddress;
                ipv4Header.UpdateChecksum();

                ref var udpHeader = ref Unsafe.As<byte, UDPHeader>(ref packetData[sizeof(EthernetHeader) + sizeof(IPv4Header)]);
                udpHeader.SourcePort = receiverPort;
                udpHeader.DestinationPort = senderPort;
                udpHeader.Size = (ushort)(sizeof(UDPHeader) + replyMessageBytes.Length);
                udpHeader.Checksum = default;

                var payload = packetData[(sizeof(EthernetHeader) + sizeof(IPv4Header) + sizeof(UDPHeader))..];
                replyMessageBytes.CopyTo(payload);
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

            using (var completed = socket.CompletionRing.Complete(256))
            {
                Assert.AreEqual(1u, completed.Length);
                using var fill = socket.FillRing.Fill(1);
                Assert.AreEqual(1u, fill.Length);
                fill[0] = completed[0];
            }
        }
    }

    [TestMethod]
    [Timeout(10000, CooperativeCancellation = true)]
    public void XdpSocket_Forward()
    {
        //const string message = "Hello from XDP client!!!";
        //const string replyMessage = "Hello from XDP server!!!";
        //var messageBytes = Encoding.ASCII.GetBytes(message);
        //var replyMessageBytes = Encoding.ASCII.GetBytes(replyMessage);
        //const int senderPort = 54321;
        //const int receiverPort = 12345;

        using var setup1 = new TrafficSetup();
        using var setup2 = new TrafficSetup(sharedSenderNs: setup1.ReceiverNs);

        using var forwardCancellation = new CancellationTokenSource();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(forwardCancellation.Token, TestContext.CancellationTokenSource.Token);

        var forwarderTask = Task.Factory.StartNew(() =>
        {
            using var forwardNs = setup1.EnterReceiver();

            using var umem = new UMemory(8192);
            Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
            umem.GetAddresses(addresses);

            var addresses1 = addresses[..(int)(umem.FrameCount / 2)];
            var addresses2 = addresses[(int)(umem.FrameCount / 2)..];

            using var socket1 = new XdpSocket(umem, setup1.ReceiverName);
            using var socket2 = new XdpSocket(umem, setup2.SenderName, shared: true);

            using (var fill = socket1.FillRing.Fill(umem.FillRingSize))
            {
                Assert.AreEqual(umem.FillRingSize, fill.Length);
                for (var i = 0u; i < fill.Length; ++i)
                    fill[i] = addresses1[(int)i];
            }

            using (var fill = socket2.FillRing.Fill(umem.FillRingSize))
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

            while (true)
            {
                var events1 = Poll.Event.Readable;
                if (packetsToSend1.Count > 0)
                    events1 |= Poll.Event.Writable;
                var events2 = Poll.Event.Readable;
                if (packetsToSend2.Count > 0)
                    events2 |= Poll.Event.Writable;
                Assert.IsTrue(XdpSocket.WaitFor([socket1, socket2], [events1, events2], nativeCancellationToken));
                ForwardOnce(socket1, socket2, packetsToSend2, addressesToFill1);
                ForwardOnce(socket2, socket1, packetsToSend1, addressesToFill2);
            }
        }, TaskCreationOptions.LongRunning);

        Thread.Sleep(200);

        forwardCancellation.Cancel();

        var ex = Assert.ThrowsExactly<AggregateException>(() => forwarderTask.Wait());
        Assert.IsInstanceOfType<OperationCanceledException>(ex.InnerException);
    }

    private static void ForwardOnce(XdpSocket sourceSocket, XdpSocket destinationSocket, Queue<XdpDescriptor> packetsToSend, Queue<ulong> addressesToFill)
    {
        using (var receivePackets = sourceSocket.RxRing.Receive(256))
        {
            for (var i = 0u; i < receivePackets.Length; ++i)
            {
                ref readonly var packet = ref receivePackets[i];
                packetsToSend.Enqueue(packet);
            }
        }
        using (var sendPackets = destinationSocket.TxRing.Send((uint)packetsToSend.Count))
        {
            for (var i = 0u; i < sendPackets.Length; ++i)
            {
                var descriptor = packetsToSend.Dequeue();
                ref var packet = ref sendPackets[i];
                packet.Address = descriptor.Address;
                packet.Length = descriptor.Length;
            }
            if (destinationSocket.TxRing.NeedsWakeup)
                destinationSocket.WakeUp();
        }
        using (var completed = sourceSocket.CompletionRing.Complete(256))
            for (var i = 0u; i < completed.Length; ++i)
                addressesToFill.Enqueue(completed[i]);
        using (var fill = sourceSocket.FillRing.Fill((uint)addressesToFill.Count))
            for (var i = 0u; i < fill.Length; ++i)
                fill[i] = addressesToFill.Dequeue();
    }

    private enum EthernetType : ushort
    {
        IPv4 = 0x0800,
        Arp = 0x0806,
        IPv6 = 0x86DD,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EthernetHeader
    {
        public MACAddress DestinationAddress;
        public MACAddress SourceAddress;
        private NetInt<ushort> _etherType;

        public EthernetType EtherType
        {
            readonly get => (EthernetType)(ushort)_etherType;
            set => _etherType = (NetInt<ushort>)(ushort)value;
        }
    }

    private enum IPProtocol : byte
    {
        ICMP = 1,
        TCP = 6,
        UDP = 17
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IPv4Header
    {
        public byte VersionAndHeaderLength;
        public byte TypeOfService;
        private NetInt<ushort> _totalLength;
        public NetInt<ushort> Id;
        public NetInt<ushort> FragmentOffset;
        public byte Ttl;
        public IPProtocol Protocol;
        private NetInt<ushort> Checksum;
        public IPv4Address SourceAddress;
        public IPv4Address DestinationAddress;

        public ushort TotalLength
        {
            readonly get => (ushort)_totalLength;
            set => _totalLength = (NetInt<ushort>)value;
        }

        public void UpdateChecksum()
        {
            Checksum = default;
            var buffer = MemoryMarshal.Cast<IPv4Header, NetInt<ushort>>(MemoryMarshal.CreateReadOnlySpan(in this, 1));
            var sum32 = 0u;
            foreach (var item in buffer)
                sum32 += (ushort)item;
            sum32 = (sum32 & 0xFFFF) + (sum32 >> 16);
            sum32 += sum32 >> 16;
            Checksum = (NetInt<ushort>)(ushort)~sum32;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct UDPHeader
    {
        private NetInt<ushort> _sourcePort;
        private NetInt<ushort> _destinationPort;
        private NetInt<ushort> _size;
        public NetInt<ushort> Checksum;

        public ushort SourcePort
        {
            readonly get => (ushort)_sourcePort;
            set => _sourcePort = (NetInt<ushort>)value;
        }

        public ushort DestinationPort
        {
            readonly get => (ushort)_destinationPort;
            set => _destinationPort = (NetInt<ushort>)value;
        }

        public ushort Size
        {
            readonly get => (ushort)_size;
            set => _size = (NetInt<ushort>)value;
        }
    }
}