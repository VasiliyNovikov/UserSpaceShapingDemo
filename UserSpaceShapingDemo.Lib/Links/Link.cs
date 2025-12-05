using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib.Nl3;
using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Lib.Links;

public class Link
{
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields")]
    protected readonly RtnlSocket Socket;
    private bool _up;
    private MACAddress _macAddress;
    private int _masterIndex;

    public int Index { get; }

    public string Name { get; }

    public bool Up
    {
        get => _up;
        set
        {
            if (_up == value)
                return;

            using var change = RtnlLink.Allocate();
            change.IfIndex = Index;
            change.Up = value;
            Socket.UpdateLink(change);
            _up = value;
        }
    }

    public MACAddress MacAddress
    {
        get => _macAddress;
        set
        {
            if (_macAddress == value)
                return;

            using var change = RtnlLink.Allocate();
            using var nlMac = new NlAddress(value.Bytes, AddressFamily.DataLink);
            change.IfIndex = Index;
            change.Address = nlMac;
            Socket.UpdateLink(change);
            _macAddress = value;
        }
    }

    public BridgeLink? Master
    {
        get
        {
            if (_masterIndex == 0)
                return null;
            if (field is null)
            {
                using var nlLink = Socket.GetLink(_masterIndex);
                field = new BridgeLink(Socket, nlLink);
            }
            return field;
        }
        set
        {
            var masterIndex = value?.Index ?? 0;
            if (_masterIndex == masterIndex)
                return;

            using var change = RtnlLink.Allocate();
            change.IfIndex = Index;
            change.Master = masterIndex;
            Socket.UpdateLink(change);
            field = value;
            _masterIndex = masterIndex;
        }
    }

    internal Link(RtnlSocket socket, RtnlLink nlLink)
    {
        Socket = socket;
        Index = nlLink.IfIndex;
        Name = nlLink.Name;
        _up = nlLink.Up;
        _macAddress = MemoryMarshal.Read<MACAddress>(nlLink.Address.Bytes);
        _masterIndex = nlLink.Master;
    }

    internal static Link Create(RtnlSocket socket, RtnlLink nlLink)
    {
        return nlLink.IsVEth
            ? new VEthLink(socket, nlLink)
            : nlLink.IsBridge
                ? new BridgeLink(socket, nlLink)
                : new Link(socket, nlLink);
    }

    public void Delete()
    {
        using var del = RtnlLink.Allocate();
        del.IfIndex = Index;
        Socket.DeleteLink(del);
    }
}