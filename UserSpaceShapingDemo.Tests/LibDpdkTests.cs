using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Dpdk;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class LibDpdkTests
{
    [TestMethod]
    public void NativeErrorNumber_DpdkLast() => Assert.AreEqual(NativeErrorNumber.OK, NativeErrorNumber.DpdkLast);

    [TestMethod]
    public void NativeErrorNumber_DpdkMessage()
    {
        Assert.AreEqual("Operation not permitted", NativeErrorNumber.OperationNotPermitted.DpdkMessage);
        Assert.AreEqual("Operation already in progress", NativeErrorNumber.OperationAlreadyInProgress.DpdkMessage);
    }
}