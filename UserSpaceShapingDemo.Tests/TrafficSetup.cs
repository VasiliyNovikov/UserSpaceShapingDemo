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
    private const byte PrefixLength4 = 30;
    private const byte PrefixLength6 = 126;
    private const string SenderNetNsNamePrefix = "tst_snd_";
    private const string ReceiverNetNsNamePrefix = "tst_rcv_";
    private static readonly ConcurrentQueue<int> InstanceIds = new(Enumerable.Range(0, 0x1000));

    public static readonly MACAddress SenderMacAddress = MACAddress.Parse("02:11:22:33:44:55");
    public static readonly MACAddress ReceiverMacAddress = MACAddress.Parse("02:66:77:88:99:AA");

    public static readonly IPAddress SenderAddress4 = IPAddress.Parse("10.11.22.1");
    public static readonly IPAddress ReceiverAddress4 = IPAddress.Parse("10.11.22.2");

    public static readonly IPAddress SenderAddress6 = IPAddress.Parse("2001:db8::1");
    public static readonly IPAddress ReceiverAddress6 = IPAddress.Parse("2001:db8::2");

    private readonly int _id;
    private readonly bool _isSharedSenderNs;
    private readonly bool _isSharedReceiverNs;

    public string SenderNs { get; }
    public string SenderName { get; }
    public string ReceiverNs { get; }
    public string ReceiverName { get; }

    public TrafficSetup(string? sharedSenderNs = null, string? sharedReceiverNs = null, byte rxQueueCount = 1, byte txQueueCount = 1)
    {
        if (!InstanceIds.TryDequeue(out _id))
            throw new InvalidOperationException("Too many instances of TestTrafficSetup");

        if (sharedSenderNs is not null && !NetNs.Exists(sharedSenderNs))
            throw new InvalidOperationException($"Shared sender namespace '{sharedSenderNs}' does not exist");

        if (sharedReceiverNs is not null && !NetNs.Exists(sharedReceiverNs))
            throw new InvalidOperationException($"Shared receiver namespace '{sharedReceiverNs}' does not exist");

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
        }

        using var senderNs = NetNs.Open(SenderNs);
        using var receiverNs = NetNs.Open(ReceiverNs);

        {
            using var collection = new LinkCollection();
            if (collection.SingleOrDefault(l => l.Name == SenderName) is { } existingLink)
                collection.Delete(existingLink);
            var (link, peer) = collection.CreateVEth(SenderName, ReceiverName, rxQueueCount, txQueueCount);
            link.MoveTo(senderNs);
            peer.MoveTo(receiverNs);
        }

        foreach (var (ns, name, address4, address6, macAddress) in new[] { (senderNs, SenderName, SenderAddress4, SenderAddress6, SenderMacAddress),
                                                                           (receiverNs, ReceiverName, ReceiverAddress4, ReceiverAddress6, ReceiverMacAddress) })
        {
            using var collection = new LinkCollection(ns);
            var link = collection[name];
            link.MacAddress = macAddress;
            link.Addresses4.Add(new(address4, PrefixLength4));
            link.Addresses6.Add(new(address6, PrefixLength6));
            link.Up = true;
        }
    }

    ~TrafficSetup() => ReleaseUnmanagedResources();

    private void ReleaseUnmanagedResources()
    {
        try
        {
            using var ns = NetNs.Open(SenderNs);
            using var collection = new LinkCollection(ns);
            var link = collection[SenderName];
            collection.Delete(link);
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
        return CreateSocket(SenderNs, socketType, protocolType, (IPAddress)SenderAddress4, port);
    }

    public Socket CreateReceiverSocket(SocketType socketType, ProtocolType protocolType, int port)
    {
        return CreateSocket(ReceiverNs, socketType, protocolType, (IPAddress)ReceiverAddress4, port);
    }

    private static Socket CreateSocket(string name, SocketType socketType, ProtocolType protocolType, IPAddress address, int port)
    {
        using (NetNs.Enter(name))
        {
            var socket = new Socket(address.AddressFamily, socketType, protocolType);
            socket.Bind(new IPEndPoint(address, port));
            return socket;
        }
    }
}