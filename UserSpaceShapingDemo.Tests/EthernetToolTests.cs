using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Links;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class EthernetToolTests
{
    [TestMethod]
    public void EthernetTool_Get_Set_Feature()
    {
        using var collection = new LinkCollection();
        var (link, _) = collection.CreateVEth("ethtool_test", "ethtool_test_p");
        try
        {

            Assert.Contains("generic-segmentation-offload: on", Script.Exec("ethtool", "-k", link.Name));
            Assert.IsTrue(EthernetTool.Get(link.Name, EthernetFeature.GSO));

            EthernetTool.Set(link.Name, EthernetFeature.GSO, false);
            Assert.Contains("generic-segmentation-offload: off", Script.Exec("ethtool", "-k", link.Name));
            Assert.IsFalse(EthernetTool.Get(link.Name, EthernetFeature.GSO));

            EthernetTool.Set(link.Name, EthernetFeature.GSO, true);
            Assert.Contains("generic-segmentation-offload: on", Script.Exec("ethtool", "-k", link.Name));
            Assert.IsTrue(EthernetTool.Get(link.Name, EthernetFeature.GSO));
        }
        finally
        {
            collection.Delete(link);
        }
    }

    [TestMethod]
    public void EthernetTool_Get_Set_Channels()
    {
        using var collection = new LinkCollection();
        var (link, _) = collection.CreateVEth("ethtool_test", "ethtool_test_p");
        try
        {
            EthernetTool.GetChannels(link.Name, out var max, out var current);
            var ethToolOutputTemplate = $$"""
                                          Channel parameters for {{link.Name}}:
                                          Pre-set maximums:
                                          RX:		{{max.RX}}
                                          TX:		{{max.TX}}
                                          Other:		{{max.Other}}
                                          Combined:	{{max.Combined}}
                                          Current hardware settings:
                                          RX:		{0}
                                          TX:		{1}
                                          Other:		{2}
                                          Combined:	{3}
                                          """;
            var expectedEthToolOutput = string.Format(CultureInfo.InvariantCulture, ethToolOutputTemplate, current.RX, current.TX, current.Other, current.Combined);
            Assert.AreEqual(expectedEthToolOutput, Script.Exec("ethtool", "-l", link.Name));

            EthernetTool.SetChannels(link.Name, rx: 3, tx: 5);
            EthernetTool.GetChannels(link.Name, out _, out var newCurrent);
            expectedEthToolOutput = string.Format(CultureInfo.InvariantCulture, ethToolOutputTemplate, newCurrent.RX, newCurrent.TX, newCurrent.Other, newCurrent.Combined);
            Assert.AreEqual(expectedEthToolOutput, Script.Exec("ethtool", "-l", link.Name));
            Assert.AreEqual(3u, newCurrent.RX);
            Assert.AreEqual(5u, newCurrent.TX);
        }
        finally
        {
            collection.Delete(link);
        }
    }
}