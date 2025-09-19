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
    private const string SenderNetNsNamePrefix = "tst_send";
    private const string ReceiverNetNsNamePrefix = "tst_recv";
    private static readonly ConcurrentBag<int> InstanceIds = [.. Enumerable.Range(0, 1000)];

    public static readonly IPAddress SenderAddress = IPAddress.Parse("10.11.22.1");
    public static readonly IPAddress ReceiverAddress = IPAddress.Parse("10.11.22.2");

    private readonly int _id;
    private readonly string _senderName;
    private readonly string _receiverName;

    public TrafficSetup()
    {
        if (!InstanceIds.TryTake(out _id))
            throw new InvalidOperationException("Too many instances of TestTrafficSetup");
        _senderName = $"{SenderNetNsNamePrefix}{_id:X}";
        _receiverName = $"{ReceiverNetNsNamePrefix}{_id:X}";
        NetNs.Add(_senderName);
        NetNs.Add(_receiverName);
        {
            using var senderNs = NetNs.Open(_senderName);
            using var receiverNs = NetNs.Open(_receiverName);
            using var socket = new RtnlSocket();
            using var vethPair = RtnlVEthPair.Allocate();
            vethPair.Link.Name = _senderName;
            vethPair.Link.NsFd = senderNs.DangerousGetHandle().ToInt32();
            vethPair.Peer.Name = _receiverName;
            vethPair.Peer.NsFd = receiverNs.DangerousGetHandle().ToInt32();
            socket.AddLink(vethPair.Link);
        }
        using (NetNs.Enter(_senderName))
        {
            using var socket = new RtnlSocket();

            using var link = socket.GetLink(_senderName);
            using var linkAddr = new RtnlAddress();
            using var addr = NlAddress.Parse($"{SenderAddress}/{PrefixLength}");
            linkAddr.IfIndex = link.IfIndex;
            linkAddr.Address = addr;
            socket.AddAddress(linkAddr);

            using var linkChange = RtnlLink.Allocate();
            linkChange.Up = true;
            socket.UpdateLink(link, linkChange);
        }

        using (NetNs.Enter(_receiverName))
        {
            using var socket = new RtnlSocket();

            using var link = socket.GetLink(_receiverName);
            using var linkAddr = new RtnlAddress();
            using var addr = NlAddress.Parse($"{ReceiverAddress}/{PrefixLength}");
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
            using (NetNs.Enter(_receiverName))
            {
                using var socket = new RtnlSocket();
                using var link = RtnlLink.Allocate();
                link.Name = _receiverName;
                socket.DeleteLink(link);
            }
        }
        finally
        {
            NetNs.Delete(_senderName);
            NetNs.Delete(_receiverName);
        }
        InstanceIds.Add(_id);
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
        using (EnterSender())
        {
            var socket = new Socket(AddressFamily.InterNetwork, socketType, protocolType);
            socket.Bind(new IPEndPoint(SenderAddress, 0));
            return socket;
        }
    }

    public Socket CreateReceiverSocket(SocketType socketType, ProtocolType protocolType, int port)
    {
        using (EnterReceiver())
        {
            var socket = new Socket(AddressFamily.InterNetwork, socketType, protocolType);
            socket.Bind(new IPEndPoint(ReceiverAddress, port));
            return socket;
        }
    }
}