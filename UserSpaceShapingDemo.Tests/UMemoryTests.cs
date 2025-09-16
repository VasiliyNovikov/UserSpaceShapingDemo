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
}
