using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Std;
using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class UMemoryTests
{
    [TestMethod]
    public void UMemory_Create_Delete()
    {
        using var fillRing = new FillRingBuffer();
        using var completionRing = new CompletionRingBuffer();
        using var umem = new UMemory(fillRing, completionRing, 4096);
    }

    [TestMethod]
    public void UMemory_Create_Argument_Error()
    {
        using var fillRing = new FillRingBuffer();
        using var completionRing = new CompletionRingBuffer();
        var exception = Assert.ThrowsExactly<XdpException>(() => new UMemory(fillRing, completionRing, 0));
        Assert.AreEqual(NativeErrorNumber.InvalidArgument, exception.ErrorNumber);
    }
}