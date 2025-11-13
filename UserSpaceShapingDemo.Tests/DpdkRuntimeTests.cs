using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Dpdk;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class DpdkRuntimeTests
{
    [TestMethod]
    public void DpdkRuntime_Initialize_Empty()
    {
        var testNs = "test-dpdk";
        NetNs.Add(testNs);
        try
        {
            using (NetNs.Enter(testNs))
            {
                using var _ = new DpdkRuntime(["test", "--no-pci", "--no-huge", "--iova=va", "--file-prefix=test",
                                               "--vdev=net_ring0", "--vdev=net_ring1"]);
            }
        }
        finally
        {
            NetNs.Delete(testNs);
        }
    }
}