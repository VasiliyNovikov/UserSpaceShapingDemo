using System;

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
        using var umem = new UMemory();
        Assert.IsNotNull(umem);
    }

    [TestMethod]
    public void UMemory_Init_Fill()
    {
        using var umem = new UMemory();
        Span<ulong> addresses = stackalloc ulong[(int)umem.FrameCount];
        umem.GetAddresses(addresses);
        using var fill = umem.FillRing.Fill(umem.FillRingSize);
        Assert.AreEqual(umem.FillRingSize, fill.Length);
        for (var i = 0u; i < fill.Length; ++i)
            fill[i] = addresses[(int)i];
    }

    [TestMethod]
    public void UMemory_Create_Argument_Error()
    {
        using var fillRing = new FillRingBuffer();
        using var completionRing = new CompletionRingBuffer();
        var exception = Assert.ThrowsExactly<XdpException>(() => new UMemory(0));
        Assert.AreEqual(NativeErrorNumber.InvalidArgument, exception.ErrorNumber);
    }
}