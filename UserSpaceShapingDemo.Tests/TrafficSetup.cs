using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using UserSpaceShapingDemo.Lib.Nl3;
using UserSpaceShapingDemo.Lib.Nl3.Route;
using UserSpaceShapingDemo.Lib.Std;

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

    public string SenderName { get; }
    public string ReceiverName { get; }

    public TrafficSetup()
    {
        if (!InstanceIds.TryDequeue(out _id))
            throw new InvalidOperationException("Too many instances of TestTrafficSetup");
        SenderName = $"{SenderNetNsNamePrefix}{_id:X}";
        ReceiverName = $"{ReceiverNetNsNamePrefix}{_id:X}";
        NetNs.Add(SenderName);
        NetNs.Add(ReceiverName);
        {
            using var senderNs = NetNs.Open(SenderName);
            using var receiverNs = NetNs.Open(ReceiverName);
            using var vethPair = RtnlVEthPair.Allocate();
            foreach (var (name, link, ns) in new[] { (SenderName, vethPair.Link, senderNs),
                                                     (ReceiverName, vethPair.Peer, receiverNs) })
            {
                link.Name = name;
                link.RxQueueCount = 1;
                link.NsDescriptor = ns.Descriptor;
            }
            using var socket = new RtnlSocket();
            socket.AddLink(vethPair.Link);
        }
        foreach (var (name, address) in new[] { (SenderName, SenderAddress),
                                                (ReceiverName, ReceiverAddress) })
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
            using (NetNs.Enter(SenderName))
            {
                using var socket = new RtnlSocket();
                using var link = RtnlLink.Allocate();
                link.Name = SenderName;
                socket.DeleteLink(link);
            }
        }
        finally
        {
            NetNs.Delete(SenderName);
            NetNs.Delete(ReceiverName);
        }
        InstanceIds.Enqueue(_id);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public NetNs.Scope EnterSender() => NetNs.Enter(SenderName);
    public NetNs.Scope EnterReceiver() => NetNs.Enter(ReceiverName);

    public Socket CreateSenderSocket(SocketType socketType, ProtocolType protocolType)
    {
        return CreateSocket(SenderName, socketType, protocolType, SenderAddress, 0);
    }

    public Socket CreateReceiverSocket(SocketType socketType, ProtocolType protocolType, int port)
    {
        return CreateSocket(ReceiverName, socketType, protocolType, ReceiverAddress, port);
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