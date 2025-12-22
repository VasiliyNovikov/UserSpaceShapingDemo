using UserSpaceShapingDemo.Lib.Nl3.Route;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class BridgeLink : Link
{
    private RtnlBridgePortState _portState;

    public RtnlBridgePortState PortState
    {
        get => _portState;
        set
        {
            if (_portState == value)
                return;
            using var change = RtnlBridgeLink.Allocate();
            change.IfIndex = Index;
            change.PortState = value;
            Socket.UpdateLink(change);
            _portState = value;
        }
    }

    internal BridgeLink(RtnlSocket socket, NetNs ns, RtnlLink nlLink)
        : base(socket, ns, nlLink)
    {
        _portState = nlLink.PortState;
    }
}