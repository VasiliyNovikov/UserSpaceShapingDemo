using UserSpaceShapingDemo.Lib.Nl3.Route;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class VEthLink : Link
{
    internal VEthLink(RtnlSocket socket, NetNs ns, RtnlLink nlLink)
        : base(socket, ns, nlLink)
    {
    }
}