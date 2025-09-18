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
            using var veth2 = socket.GetLink(vethName);
            using var vethPeer2 = socket.GetLink(vethPeerName);

            Assert.IsGreaterThan(0, veth2.IfIndex);
            Assert.IsGreaterThan(0, vethPeer2.IfIndex);

            Assert.AreEqual(vethName, veth2.Name);
            Assert.AreEqual(vethPeerName, vethPeer2.Name);

            Script.Exec("ip", "link", "show", vethName);
            Script.Exec("ip", "link", "show", vethPeerName);

            using var vethChange = RtnlLink.Allocate();
            vethChange.Up = true;
            socket.Update(veth2, vethChange);

            using var vethPeerChange = RtnlLink.Allocate();
            vethPeerChange.Up = true;
            socket.Update(vethPeer2, vethPeerChange);

            //Thread.Sleep(5000);

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