using System.Net;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Nl3;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class NlAddressTests
{
    [TestMethod]
    public void NlAddress_Create_ToString()
    {
        var addressString = "192.168.0.1";
        var ipAddress = IPAddress.Parse(addressString);
        using var address = new NlAddress(ipAddress);
        Assert.AreEqual(addressString, address.ToString());
    }

    [TestMethod]
    [DataRow("192.168.0.1")]
    [DataRow("2001:db8:85a3::8a2e:370:7334")]
    [DataRow("fe80::1ff:fe23:4567:890a")]
    [DataRow("192.168.0.1/30")]
    [DataRow("::1")]
    [DataRow("::1/127")]
    [DataRow("11:22:33:44:55:66")]
    [DataRow("11:22:33:44:55:66/40")]
    public void NlAddress_Parse_ToString(string addressString)
    {
        using var address = NlAddress.Parse(addressString);
        Assert.AreEqual(addressString, address.ToString());
    }
}