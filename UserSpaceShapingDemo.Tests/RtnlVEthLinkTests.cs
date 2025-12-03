using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Nl3;
using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class RtnlVEthLinkTests
{
    [TestMethod]
    public void RtnlVEthLink_Alloc_Add_Add_Addr_Delete()
    {
        const string vethName = "test_veth0";
        const string vethPeerName = "test_veth1";
        const string vethAddress = "10.0.10.1/30";
        const string vethPeerAddress = "10.0.10.2/30";

        using var socket = new RtnlSocket();
        using var vethPair = RtnlVEthPair.Allocate();
        vethPair.Link.Name = vethName;
        vethPair.Peer.Name = vethPeerName;

        Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethName));
        Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethPeerName));

        socket.AddLink(vethPair.Link);
        try
        {
            var linkInfo = Script.Exec("ip", "link", "show", vethName);
            Assert.AreNotEqual("", linkInfo);
            Assert.Contains(vethName, linkInfo);
            Assert.Contains("veth", linkInfo);
            Assert.Contains("DOWN", linkInfo);

            var peerInfo = Script.Exec("ip", "link", "show", vethPeerName);
            Assert.AreNotEqual("", peerInfo);
            Assert.Contains(vethPeerName, peerInfo);
            Assert.Contains("veth", peerInfo);
            Assert.Contains("DOWN", peerInfo);

            using var veth = socket.GetLink(vethName);
            using var vethPeer = socket.GetLink(vethPeerName);

            Assert.IsGreaterThan(0, veth.IfIndex);
            Assert.IsGreaterThan(0, vethPeer.IfIndex);

            Assert.AreEqual(vethName, veth.Name);
            Assert.AreEqual(vethPeerName, vethPeer.Name);

            using var vethLinkAddr = new RtnlAddress();
            using var vethAddr = NlAddress.Parse(vethAddress);
            vethLinkAddr.IfIndex = veth.IfIndex;
            vethLinkAddr.Address = vethAddr;
            socket.AddAddress(vethLinkAddr);

            Assert.Contains(vethAddress, Script.Exec("ip", "address", "show", vethName));

            using var vethPeerLinkAddr = new RtnlAddress();
            using var vethPeerAddr = NlAddress.Parse(vethPeerAddress);
            vethPeerLinkAddr.IfIndex = vethPeer.IfIndex;
            vethPeerLinkAddr.Address = vethPeerAddr;
            socket.AddAddress(vethPeerLinkAddr);

            Assert.Contains(vethPeerAddress, Script.Exec("ip", "address", "show", vethPeerName));

            using var vethChange = RtnlLink.Allocate();
            vethChange.IfIndex = veth.IfIndex;
            vethChange.Up = true;
            socket.UpdateLink(vethChange);

            using var vethPeerChange = RtnlLink.Allocate();
            vethPeerChange.IfIndex = vethPeer.IfIndex;
            vethPeerChange.Up = true;
            socket.UpdateLink(vethPeerChange);

            Assert.Contains("UP", Script.Exec("ip", "address", "show", vethName));
            Assert.Contains("UP", Script.Exec("ip", "address", "show", vethPeerName));

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