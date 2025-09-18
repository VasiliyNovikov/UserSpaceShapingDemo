using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class RtnlVEthLinkTests
{
    [TestMethod]
    public void RtnlVEthLink_Alloc_Add_Delete()
    {
        const string vethName = "test_veth0";
        const string vethPeerName = "test_veth1";

        using var socket = new RtnlSocket();
        using var veth = RtnlVEthLink.Allocate();
        veth.Name = vethName;
        veth.Peer.Name = vethPeerName;
        socket.Add(veth);
        try
        {
            Script.Exec("ip", "link", "show", vethName);
            Script.Exec("ip", "link", "show", vethPeerName);

            socket.Delete(veth);

            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethName));
            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethPeerName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "link", "del", vethName);
        }
    }
}