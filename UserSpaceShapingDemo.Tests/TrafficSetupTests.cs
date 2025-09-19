using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class TrafficSetupTests
{
    [TestMethod]
    public void TrafficSetup_Create_Destroy()
    {
        using var trafficSetup = new TrafficSetup();
        Assert.IsNotNull(trafficSetup);
    }
}