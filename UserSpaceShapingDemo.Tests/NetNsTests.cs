using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using UserSpaceShapingDemo.Lib;

namespace UserSpaceShapingDemo.Tests;

[TestClass]
public class NetNsTests
{
    [TestMethod]
    public void NetNs_Add()
    {
        const string testNsName = "test_ns";
        try
        {
            NetNs.Add(testNsName);
            Assert.IsTrue(IsNetNsExists(testNsName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "netns", "delete", testNsName);
        }
    }

    [TestMethod]
    public void NetNs_Delete()
    {
        const string testNsName = "test_ns_2";
        try
        {
            Script.Exec("ip", "netns", "add", testNsName);
            NetNs.Delete(testNsName);
            Assert.IsFalse(IsNetNsExists(testNsName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "netns", "delete", testNsName);
        }
    }

    [TestMethod]
    public void NetNs_Exists()
    {
        const string testNsName = "test_ns_3";
        Assert.IsFalse(NetNs.Exists(testNsName));
        try
        {
            Script.Exec("ip", "netns", "add", testNsName);
            Assert.IsTrue(NetNs.Exists(testNsName));
            Script.Exec("ip", "netns", "delete", testNsName);
            Assert.IsFalse(NetNs.Exists(testNsName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "netns", "delete", testNsName);
        }
    }

    private static bool IsNetNsExists(string nsName)
    {
        return Script.ExecLines("ip", "netns", "list").Any(n => n.StartsWith(nsName, StringComparison.Ordinal));
    }
}