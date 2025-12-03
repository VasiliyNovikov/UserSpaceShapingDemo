using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Nl3;
using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class RtnlBridgeLinkTests
{
    [TestMethod]
    public void RtnlBridgeLink_Alloc_Add_Delete()
    {
        const string bridgeName = "test_br0";
        const string bridgeAddress = "10.0.10.1/30";

        using var socket = new RtnlSocket();
        using var bridge = RtnlBridgeLink.Allocate();
        bridge.Name = bridgeName;

        Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", bridgeName));

        socket.AddLink(bridge);
        try
        {
            var linkInfo = Script.Exec("ip", "link", "show", bridgeName);
            Assert.AreNotEqual("", linkInfo);
            Assert.Contains(bridgeName, linkInfo);
            Assert.Contains("DOWN", linkInfo);

            using var addedBridge = socket.GetLink(bridgeName);
            Assert.IsGreaterThan(0, addedBridge.IfIndex);
            Assert.AreEqual(bridgeName, addedBridge.Name);
            Assert.IsTrue(addedBridge.IsBridge);
            Assert.IsFalse(addedBridge.Up);
            Assert.AreEqual(RtnlBridgePortState.Disabled, addedBridge.State);

            using var bridgeLinkAddr = RtnlAddress.Alloc();
            using var bridgeAddr = NlAddress.Parse(bridgeAddress);
            bridgeLinkAddr.IfIndex = addedBridge.IfIndex;
            bridgeLinkAddr.Address = bridgeAddr;
            socket.AddAddress(bridgeLinkAddr);

            Assert.Contains(bridgeAddress, Script.Exec("ip", "address", "show", bridgeName));

            using var bridgeChange = RtnlLink.Allocate();
            bridgeChange.IfIndex = addedBridge.IfIndex;
            bridgeChange.Up = true;
            socket.UpdateLink(bridgeChange);

            linkInfo = Script.Exec("ip", "link", "show", bridgeName);
            Assert.Contains("UP", linkInfo);

            using var changedBridge = socket.GetLink(bridgeName);
            Assert.IsTrue(changedBridge.Up);
            Assert.AreEqual(RtnlBridgePortState.Disabled, changedBridge.State);

            socket.DeleteLink(addedBridge);

            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", bridgeName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "link", "del", bridgeName);
        }
    }
}