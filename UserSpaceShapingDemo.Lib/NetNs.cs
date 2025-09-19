using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib;

public static unsafe class NetNs
{
    private const string NetNsBasePath = "/run/netns";
    private const UnixFileMode NetNsBasePathMode = (UnixFileMode)0755;
    private const UnixFileMode NetNsFileMode = (UnixFileMode)0644;
    private const string SelfThreadNsNetPath = "/proc/thread-self/ns/net";
    private const string SelfThreadFdPath = "/proc/thread-self/fd";
    private const string RootNsNetPath = "/proc/1/ns/net";

    public static void Add(string name)
    {
        // Ensure the base path exists
        Directory.CreateDirectory(NetNsBasePath, NetNsBasePathMode);

        // Keep a handle to the original netns so we can switch back later
        using var oldNsFd = OpenCurrent();

        var target = Path.Combine(NetNsBasePath, name);
        try
        {
            // Create the target file (regular file is fine) that we'll bind-mount onto
            using (var targetFd = File.OpenHandle(target, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                File.SetUnixFileMode(targetFd, NetNsFileMode);

            // Create a new network namespace for *this thread*
            if (LibC.unshare(LibC.CLONE_NEWNET) < 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            try
            {
                // Open a handle to the *new* netns we just created
                using var nsFd = OpenCurrent();

                // Bind-mount the new netns to /run/netns/<name> to persist it.
                // Using the /proc/thread-self/fd/<nsFd> path ensures we bind the FD we opened.
                using var nsFdRef = nsFd.Ref();
                var srcPath = Path.Combine(SelfThreadFdPath, nsFdRef.ToString());
                if (LibC.mount(srcPath, target, null, LibC.MS_BIND, null) < 0)
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            finally
            {
                Set(oldNsFd);
            }
        }
        catch
        {
            if (File.Exists(target))
                File.Delete(target);
            throw;
        }
    }

    public static void Delete(string name)
    {
        var target = Path.Combine(NetNsBasePath, name);
        // Unmount the netns file
        if (LibC.umount2(target, LibC.MNT_DETACH) < 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        // Delete the file
        File.Delete(target);
    }

    public static bool Exists(string name) => File.Exists(Path.Combine(NetNsBasePath, name));

    public static string[] List() => Directory.Exists(NetNsBasePath) ? Directory.GetFiles(NetNsBasePath) : [];

    public static Scope Enter(string name) => new(Path.Combine(NetNsBasePath, name));

    public static Scope EnterRoot() => new(RootNsNetPath);

    private static SafeFileHandle OpenPath(string path) => File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    private static SafeFileHandle OpenCurrent() => OpenPath(SelfThreadNsNetPath);

    public static SafeFileHandle Open(string name) => OpenPath(Path.Combine(NetNsBasePath, name));

    private static void Set(SafeFileHandle nsFd)
    {
        using var fdRef = nsFd.Ref();
        if (LibC.setns(fdRef, LibC.CLONE_NEWNET) < 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    public sealed class Scope : IDisposable
    {
        private readonly SafeFileHandle _oldNsFd;

        internal Scope(string path)
        {
            // Keep a handle to the original netns so we can switch back later
            _oldNsFd = OpenCurrent();

            // Open a handle to the target netns
            using var targetFd = OpenPath(path);

            // Switch this thread to the target netns
            Set(targetFd);
        }

        public void Dispose()
        {
            try
            {
                Set(_oldNsFd);
            }
            finally
            {
                _oldNsFd.Dispose();
            }
        }
    }
}