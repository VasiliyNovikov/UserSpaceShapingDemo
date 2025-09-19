using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Lib.Nl3;
using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Tests;

public sealed class TrafficSetup : IDisposable
{
    private const byte PrefixLength = 30;
    private const string SenderNetNsNamePrefix = "tst_snd_";
    private const string ReceiverNetNsNamePrefix = "tst_rcv_";
    private static readonly ConcurrentQueue<int> InstanceIds = new(Enumerable.Range(0, 0x1000));

    public static readonly IPAddress SenderAddress = IPAddress.Parse("10.11.22.1");
    public static readonly IPAddress ReceiverAddress = IPAddress.Parse("10.11.22.2");

    private readonly int _id;
    private readonly string _senderName;
    private readonly string _receiverName;

    public TrafficSetup()
    {
        if (!InstanceIds.TryDequeue(out _id))
            throw new InvalidOperationException("Too many instances of TestTrafficSetup");
        _senderName = $"{SenderNetNsNamePrefix}{_id:X}";
        _receiverName = $"{ReceiverNetNsNamePrefix}{_id:X}";
        NetNs.Add(_senderName);
        NetNs.Add(_receiverName);
        {
            using var senderNs = NetNs.Open(_senderName);
            using var receiverNs = NetNs.Open(_receiverName);
            using var vethPair = RtnlVEthPair.Allocate();
            foreach (var (name, link, ns) in new[] { (_senderName, vethPair.Link, senderNs),
                                                     (_receiverName, vethPair.Peer, receiverNs) })
            {
                link.Name = name;
                link.RxQueueCount = 1;
                using var nsRef = ns.Ref();
                link.NsFd = nsRef;
            }
            using var socket = new RtnlSocket();
            socket.AddLink(vethPair.Link);
        }
        foreach (var (name, address) in new[] { (_senderName, SenderAddress),
                                                (_receiverName, ReceiverAddress) })
            using (NetNs.Enter(name))
            {
                using var socket = new RtnlSocket();

                using var link = socket.GetLink(name);
                using var linkAddr = new RtnlAddress();
                using var addr = NlAddress.Parse($"{address}/{PrefixLength}");
                linkAddr.IfIndex = link.IfIndex;
                linkAddr.Address = addr;
                socket.AddAddress(linkAddr);

                using var linkChange = RtnlLink.Allocate();
                linkChange.Up = true;
                socket.UpdateLink(link, linkChange);
            }
    }

    ~TrafficSetup() => ReleaseUnmanagedResources();

    private void ReleaseUnmanagedResources()
    {
        try
        {
            using (NetNs.Enter(_senderName))
            {
                using var socket = new RtnlSocket();
                using var link = RtnlLink.Allocate();
                link.Name = _senderName;
                socket.DeleteLink(link);
            }
        }
        finally
        {
            NetNs.Delete(_senderName);
            NetNs.Delete(_receiverName);
        }
        InstanceIds.Enqueue(_id);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public NetNs.Scope EnterSender() => NetNs.Enter(_senderName);
    public NetNs.Scope EnterReceiver() => NetNs.Enter(_receiverName);

    public Socket CreateSenderSocket(SocketType socketType, ProtocolType protocolType)
    {
        return CreateSocket(_senderName, socketType, protocolType, SenderAddress, 0);
    }

    public Socket CreateReceiverSocket(SocketType socketType, ProtocolType protocolType, int port)
    {
        return CreateSocket(_receiverName, socketType, protocolType, ReceiverAddress, port);
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