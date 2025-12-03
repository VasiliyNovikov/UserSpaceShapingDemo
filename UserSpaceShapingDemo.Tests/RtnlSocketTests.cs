using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Nl3.Route;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class RtnlSocketTests
{
    [TestMethod]
    public void RtnlSocket_Open_Close()
    {
        using var socket = new RtnlSocket();
        Assert.IsNotNull(socket);
    }

    [TestMethod]
    public void RtnlSocket_GetLink()
    {
        using var socket = new RtnlSocket();
        using var link = socket.GetLink("lo");
        Assert.IsGreaterThan(0, link.IfIndex);
        Assert.AreEqual("lo", link.Name);
    }

    [TestMethod]
    public void RtnlSocket_GetLinks()
    {
        using var socket = new RtnlSocket();
        using var links = socket.GetLinks();
        Assert.IsGreaterThan(2, links.Count);
        var lo = links.FirstOrDefault(l => l.Name == "lo");
        Assert.IsNotNull(lo);
        Assert.IsGreaterThan(0, lo.IfIndex);
    }
}