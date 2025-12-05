using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NetworkingPrimitivesCore;

using UserSpaceShapingDemo.Lib.Links;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class LinkTests
{
    [TestMethod]
    public void LinkCollection_Open_Close()
    {
        using var collection = new LinkCollection();
        Assert.IsNotNull(collection);
    }

    [TestMethod]
    public void LinkCollection_GetLink()
    {
        using var collection = new LinkCollection();
        var link = collection["lo"];
        Assert.IsGreaterThan(0, link.Index);
        Assert.AreEqual("lo", link.Name);
        Assert.AreEqual(default, link.MacAddress);

        var ipV4Addrs = link.IPv4Addresses.ToArray();
        Assert.HasCount(1, ipV4Addrs);
        Assert.AreEqual(IPv4Address.Loopback, ipV4Addrs[0].Address);
        Assert.AreEqual(8, ipV4Addrs[0].PrefixLength);

        var ipV6Addrs = link.IPv6Addresses.ToArray();
        Assert.HasCount(1, ipV6Addrs);
        Assert.AreEqual(IPv6Address.Loopback, ipV6Addrs[0].Address);
        Assert.AreEqual(128, ipV6Addrs[0].PrefixLength);
    }

    [TestMethod]
    public void LinkCollection_GetLinks()
    {
        using var collection = new LinkCollection();
        var links = collection.ToArray();
        Assert.IsGreaterThan(1, links.Length);
        var lo = links.FirstOrDefault(l => l.Name == "lo");
        Assert.IsNotNull(lo);
        Assert.IsGreaterThan(0, lo.Index);
    }
}