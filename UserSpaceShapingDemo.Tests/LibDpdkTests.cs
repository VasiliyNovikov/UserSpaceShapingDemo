using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class LibDpdkTests
{
    [TestMethod]
    public void LibDpdk_rte_errno() => Assert.AreEqual(0, LibDpdk.rte_errno);
}