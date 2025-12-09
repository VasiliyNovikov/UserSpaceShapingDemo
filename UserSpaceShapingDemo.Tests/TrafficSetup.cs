using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib.Links;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Tests;

public sealed class TrafficSetup : IDisposable
{
    private const byte PrefixLength = 30;
    private const string SenderNetNsNamePrefix = "tst_snd_";
    private const string ReceiverNetNsNamePrefix = "tst_rcv_";
    private static readonly ConcurrentQueue<int> InstanceIds = new(Enumerable.Range(0, 0x1000));

    public static readonly MACAddress SenderMacAddress = MACAddress.Parse("02:11:22:33:44:55");
    public static readonly MACAddress ReceiverMacAddress = MACAddress.Parse("02:66:77:88:99:AA");

    public static readonly IPAddress SenderAddress = IPAddress.Parse("10.11.22.1");
    public static readonly IPAddress ReceiverAddress = IPAddress.Parse("10.11.22.2");

    private readonly int _id;
    private readonly bool _isSharedSenderNs;
    private readonly bool _isSharedReceiverNs;

    public string SenderNs { get; }
    public string SenderName { get; }
    public string ReceiverNs { get; }
    public string ReceiverName { get; }

    public TrafficSetup(string? sharedSenderNs = null, string? sharedReceiverNs = null)
    {
        if (!InstanceIds.TryDequeue(out _id))
            throw new InvalidOperationException("Too many instances of TestTrafficSetup");

        SenderName = $"{SenderNetNsNamePrefix}{_id:X}";
        ReceiverName = $"{ReceiverNetNsNamePrefix}{_id:X}";
        if (sharedSenderNs is null)
        {
            _isSharedSenderNs = false;
            SenderNs = SenderName;
            NetNs.ReCreate(SenderNs);
        }
        else
        {
            _isSharedSenderNs = true;
            SenderNs = sharedSenderNs;
            if (!NetNs.Exists(SenderNs))
                throw new InvalidOperationException($"Shared sender namespace '{sharedSenderNs}' does not exist");
        }

        if (sharedReceiverNs is null)
        {
            _isSharedReceiverNs = false;
            ReceiverNs = ReceiverName;
            NetNs.ReCreate(ReceiverNs);
        }
        else
        {
            _isSharedReceiverNs = true;
            ReceiverNs = sharedReceiverNs;
            if (!NetNs.Exists(ReceiverNs))
                throw new InvalidOperationException($"Shared receiver namespace '{sharedReceiverNs}' does not exist");
        }

        {
            using var collection = new LinkCollection();
            using var senderNs = NetNs.Open(SenderNs);
            using var receiverNs = NetNs.Open(ReceiverNs);
            var vethPair = collection.CreateVEth(SenderName, ReceiverName, 1);
            vethPair.Link.MoveTo(senderNs);
            vethPair.Peer.MoveTo(receiverNs);
        }
        foreach (var (ns, name, address, macAddress) in new[] { (SenderNs, SenderName, SenderAddress, SenderMacAddress),
                                                                (ReceiverNs, ReceiverName, ReceiverAddress, ReceiverMacAddress) })
            using (NetNs.Enter(ns))
            {
                using var collection = new LinkCollection();
                var link = collection[name];
                link.MacAddress = macAddress;
                link.IPv4Addresses.Add(new(address, PrefixLength));
                link.Up = true;
            }
    }

    ~TrafficSetup() => ReleaseUnmanagedResources();

    private void ReleaseUnmanagedResources()
    {
        try
        {
            using (NetNs.Enter(SenderNs))
            {
                using var collection = new LinkCollection();
                var link = collection[SenderName];
                collection.Delete(link);
            }
        }
        finally
        {
            if (!_isSharedSenderNs)
                NetNs.Delete(SenderNs);
            if (!_isSharedReceiverNs)
                NetNs.Delete(ReceiverNs);
        }
        InstanceIds.Enqueue(_id);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public NetNs.Scope EnterSender() => NetNs.Enter(SenderNs);
    public NetNs.Scope EnterReceiver() => NetNs.Enter(ReceiverNs);

    public Socket CreateSenderSocket(SocketType socketType, ProtocolType protocolType, int port = 0)
    {
        return CreateSocket(SenderNs, socketType, protocolType, SenderAddress, port);
    }

    public Socket CreateReceiverSocket(SocketType socketType, ProtocolType protocolType, int port)
    {
        return CreateSocket(ReceiverNs, socketType, protocolType, ReceiverAddress, port);
    }

    private static Socket CreateSocket(string name, SocketType socketType, ProtocolType protocolType, IPAddress address, int port)
    {
        using (NetNs.Enter(name))
        {
            var socket = new Socket(AddressFamily.InterNetwork, socketType, protocolType);
            socket.Bind(new IPEndPoint(address, port));
            return socket;
        }
    }
}