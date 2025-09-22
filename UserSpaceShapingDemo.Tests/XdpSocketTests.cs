using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public sealed class XdpSocketTests
{
    [TestMethod]
    public void XdpSocket_Open_Close()
    {
        using var setup = new TrafficSetup();
        using (setup.EnterReceiver())
        {
            using var rxBuffer = new RxRingBuffer();
            using var txBuffer = new TxRingBuffer();
            using var fillRing = new FillRingBuffer();
            using var completionRing = new CompletionRingBuffer();
            using var umem = new UMemory(fillRing, completionRing, 4096);
            using var socket = new XdpSocket(setup.ReceiverName, 0, umem, rxBuffer, txBuffer, 2048, 2048, XdpSocketMode.Generic);
            Assert.IsNotNull(socket);
        }
    }

    [TestMethod]
    public void XdpSocket_Open_Fill_Close()
    {
        using var setup = new TrafficSetup();
        using (setup.EnterReceiver())
        {
            using var rxBuffer = new RxRingBuffer();
            using var txBuffer = new TxRingBuffer();
            using var fillRing = new FillRingBuffer();
            using var completionRing = new CompletionRingBuffer();
            using var umem = new UMemory(fillRing, completionRing, 4096);
            using var socket = new XdpSocket(setup.ReceiverName, 0, umem, rxBuffer, txBuffer, 2048, 2048, XdpSocketMode.Generic);
            Assert.IsNotNull(socket);
        }
    }
}