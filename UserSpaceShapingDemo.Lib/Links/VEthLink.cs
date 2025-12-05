using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Lib.Links;

public sealed class VEthLink : Link
{
    internal VEthLink(RtnlSocket socket, RtnlLink nlLink)
        : base(socket, nlLink)
    {
    }
}