using NetNsCore;

using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class VEthLink : Link
{
    internal VEthLink(RtnlSocket socket, NetNs ns, RtnlLink nlLink)
        : base(socket, ns, nlLink)
    {
    }
}