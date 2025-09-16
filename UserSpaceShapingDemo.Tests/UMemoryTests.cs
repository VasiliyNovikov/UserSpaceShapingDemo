using System.ComponentModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class UMemoryTests
{
    [TestMethod]
    public void UMemory_Create_Delete()
    {
        using var fillRing = new ProducerRingBuffer();
        using var completionRing = new ConsumerRingBuffer();
        using var umem = new UMemory(fillRing, completionRing, 4096);
    }

    [TestMethod]
    public void UMemory_Create_Argument_Error()
    {
        using var fillRing = new ProducerRingBuffer();
        using var completionRing = new ConsumerRingBuffer();
        var exception = Assert.ThrowsExactly<Win32Exception>(() => new UMemory(fillRing, completionRing, 0));
        Assert.AreEqual(22, exception.NativeErrorCode);
    }
}