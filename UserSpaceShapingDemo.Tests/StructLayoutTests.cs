using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class StructLayoutTests
{
    [TestMethod]
    public void Test_XdpDescriptor_Layout() => Assert.AreEqual(16, Marshal.SizeOf<XdpDescriptor>());
}