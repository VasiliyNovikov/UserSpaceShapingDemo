using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class RtnlVEthLinkTests
{
    [TestMethod]
    public void RtnlVEthLink_Alloc_Add_Delete()
    {
        using var socket = new RtnlSocket();
        using var veth = RtnlVEthLink.Allocate();
        veth.Name = "test_veth0";
        veth.Peer.Name = "test_veth1";
        socket.Add(veth);
        socket.Delete(veth);
    }
}