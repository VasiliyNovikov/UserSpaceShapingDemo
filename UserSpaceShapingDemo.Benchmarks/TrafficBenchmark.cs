using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

using UserSpaceShapingDemo.Tests;

namespace UserSpaceShapingDemo.Benchmarks;

public abstract class TrafficBenchmark(int version)
{
    protected const int SenderPort = 5000;
    protected const int ReceiverPort = 6000;
    private const int BatchSize = 32;
    private const int FlowSize = 1024;
    private const int SocketBufferSize = 32;

    private static readonly byte[] Packet = new byte[1400];
    private static readonly byte[] PacketBuffer = new byte[2048];

    static TrafficBenchmark() => RandomNumberGenerator.Fill(Packet);

    private readonly SocketAddress _receiverAddress = new IPEndPoint(TrafficSetup.ReceiverAddress(version), ReceiverPort).Serialize();
    private readonly SocketAddress _addressBuffer = new IPEndPoint(version == 4 ? IPAddress.Any : IPAddress.IPv6Any, 0).Serialize();

    protected abstract Socket Sender
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    protected abstract Socket Receiver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendOne()
    {
        Sender.SendTo(Packet, SocketFlags.None, _receiverAddress);
        Receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, _addressBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendBatch()
    {
        var sender = Sender;
        var receiver = Receiver;
        for (var i = 0; i < BatchSize; ++i)
            sender.SendTo(Packet, SocketFlags.None, _receiverAddress);
        for (var i = 0; i < BatchSize; ++i)
            receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, _addressBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendFlow()
    {
        var sender = Sender;
        var receiver = Receiver;
        var sendIndex = 0;
        var receiveIndex = 0;
        while (sendIndex < FlowSize || receiveIndex < FlowSize)
        {
            if (sendIndex++ < FlowSize)
                sender.SendTo(Packet, SocketFlags.None, _receiverAddress);
            if (sendIndex >= SocketBufferSize && receiveIndex++ < FlowSize)
                receiver.ReceiveFrom(PacketBuffer, SocketFlags.None, _addressBuffer);
        }
    }
}