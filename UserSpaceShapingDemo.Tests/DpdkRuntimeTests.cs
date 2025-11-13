using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Dpdk;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class DpdkRuntimeTests
{
    [TestMethod]
    public void DpdkRuntime_Initialize_Empty()
    {
        using var _ = new DpdkRuntime(["--in-memory", "--iova=va", "--file-prefix=test"]);
    }
}