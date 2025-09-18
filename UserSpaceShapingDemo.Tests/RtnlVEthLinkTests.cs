using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Nl3;
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
        socket.AddLink(veth);
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

            using var vethLinkAddr = new RtnlAddress();
            using var vethAddr = NlAddress.Parse("10.0.10.1/30");
            vethLinkAddr.IfIndex = veth2.IfIndex;
            vethLinkAddr.Address = vethAddr;
            socket.AddAddress(vethLinkAddr);

            using var vethPeerLinkAddr = new RtnlAddress();
            using var vethPeerAddr = NlAddress.Parse("10.0.10.2/30");
            vethPeerLinkAddr.IfIndex = vethPeer2.IfIndex;
            vethPeerLinkAddr.Address = vethPeerAddr;
            socket.AddAddress(vethPeerLinkAddr);

            using var vethChange = RtnlLink.Allocate();
            vethChange.Up = true;
            socket.UpdateLink(veth2, vethChange);

            using var vethPeerChange = RtnlLink.Allocate();
            vethPeerChange.Up = true;
            socket.UpdateLink(vethPeer2, vethPeerChange);

            //Thread.Sleep(10000);

            socket.DeleteLink(veth);

            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethName));
            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethPeerName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "link", "del", vethName);
        }
    }
}