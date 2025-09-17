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
}