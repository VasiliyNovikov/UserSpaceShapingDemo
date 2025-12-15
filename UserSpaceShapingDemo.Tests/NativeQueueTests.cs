using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib;
using UserSpaceShapingDemo.Lib.Forwarding;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class NativeQueueTests
{
    [TestMethod]
    public void NativeQueue_Enqueue_Dequeue()
    {
        var queue = new NativeQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        Assert.AreEqual(1, queue.Dequeue());
        Assert.AreEqual(2, queue.Dequeue());
        Assert.AreEqual(3, queue.Dequeue());

        Assert.IsFalse(queue.TryDequeue(out _));
    }

    [TestMethod]
    public void NativeQueue_Poll()
    {
        using var queue = new NativeQueue<int>();
        List<int> dequeuedItems = [];

        var dequeueCancellation = new CancellationTokenSource(); 
        var dequeueTask = Task.Run(() =>
        {
            using var nativeCancellationToken = new NativeCancellationToken(dequeueCancellation.Token);
            try
            {
                while (true)
                {
                    nativeCancellationToken.Wait(queue, Poll.Event.Readable);
                    Assert.IsTrue(queue.TryDequeue(out var item));
                    dequeuedItems.Add(item);
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken == dequeueCancellation.Token)
            {
            }
        });

        Thread.Sleep(10);
        queue.Enqueue(10);
        Thread.Sleep(20);
        queue.Enqueue(20);
        Thread.Sleep(30);
        queue.Enqueue(30);
        Thread.Sleep(40);
        dequeueCancellation.Cancel();
        dequeueTask.Wait();
        CollectionAssert.AreEqual(new List<int> { 10, 20, 30 }, dequeuedItems);
    }
}