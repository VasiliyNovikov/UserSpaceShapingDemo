using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using LinuxCore;

using NetNsCore;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib.Nl3;
using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Lib.Links;

public class Link
{
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields")]
    protected readonly RtnlSocket Socket;
    private readonly NetNs _ns;
    private bool _up;
    private MACAddress? _macAddress;
    private int _masterIndex;

    public int Index { get; }

    public string Name { get; }

    public uint RXQueueCount { get; }

    public uint TXQueueCount { get; }

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

    public BridgeLink? Master
    {
        get
        {
            if (_masterIndex == 0)
                return null;
            if (field is null)
            {
                using var nlLink = Socket.GetLink(_masterIndex);
                field = new BridgeLink(Socket, _ns, nlLink);
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

    public MACAddress? MacAddress
    {
        get => _macAddress;
        set
        {
            if (_macAddress == value)
                return;

            using var change = RtnlLink.Allocate();
            change.IfIndex = Index;
            if (value is null)
                change.Address = null;
            else
            {
                using var nlMac = new NlAddress(value!.Value.Bytes, LinuxAddressFamily.LLC);
                change.Address = nlMac;
            }
            Socket.UpdateLink(change);
            _macAddress = value;
        }
    }

    public LinkAddressCollection<IPv4Address> Addresses4 => field ??= new(Socket, Index);

    public LinkAddressCollection<IPv6Address> Addresses6 => field ??= new(Socket, Index);

    public LinkHardwareOffload Offload => field ??= new(_ns, Name);

    internal Link(RtnlSocket socket, NetNs ns, RtnlLink nlLink)
    {
        Socket = socket;
        _ns = ns;
        Index = nlLink.IfIndex;
        Name = nlLink.Name;
        RXQueueCount = nlLink.RXQueueCount;
        TXQueueCount = nlLink.TXQueueCount;
        _up = nlLink.Up;
        var nlMac = nlLink.Address;
        _macAddress = nlMac is null ? null : MemoryMarshal.Read<MACAddress>(nlMac.Bytes);
        _masterIndex = nlLink.Master;
    }

    internal static Link Create(RtnlSocket socket, NetNs ns, RtnlLink nlLink)
    {
        return nlLink.IsVEth
            ? new VEthLink(socket, ns, nlLink)
            : nlLink.IsBridge
                ? new BridgeLink(socket, ns, nlLink)
                : new Link(socket, ns, nlLink);
    }

    public void MoveTo(NetNs ns)
    {
        using var change = RtnlLink.Allocate();
        change.IfIndex = Index;
        change.NsDescriptor = ns.Descriptor;
        Socket.UpdateLink(change);
    }
}