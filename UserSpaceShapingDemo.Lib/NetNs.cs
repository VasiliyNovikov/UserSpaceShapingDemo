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
    private const string NetNsSelfPath = "/proc/self/ns/net";
    private const UnixFileMode NetNsFileMode = (UnixFileMode)0644;

    public static void Add(string name)
    {
        // Ensure the base path exists
        Directory.CreateDirectory(NetNsBasePath, NetNsBasePathMode);

        // Keep a handle to the original netns so we can switch back later
        using var oldNsFd = File.OpenHandle(NetNsSelfPath, FileMode.Open, FileAccess.Read, FileShare.Read);

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
                using var nsFd = File.OpenHandle(NetNsSelfPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                // Bind-mount the new netns to /run/netns/<name> to persist it.
                // Using the /proc/self/fd/<nsFd> path ensures we bind the FD we opened.
                var srcPath = $"/proc/self/fd/{nsFd.DangerousGetHandle().ToInt32()}";
                if (LibC.mount(srcPath, target, null, LibC.MS_BIND, null) < 0)
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            finally
            {
#pragma warning disable CA2219
                if (LibC.setns(oldNsFd.DangerousGetHandle().ToInt32(), LibC.CLONE_NEWNET) < 0)
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
#pragma warning restore CA2219
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

    public static Scope Enter(string name) => new(name);

    public sealed class Scope : IDisposable
    {
        private readonly SafeFileHandle _oldNsFd;

        internal Scope(string name)
        {
            // Keep a handle to the original netns so we can switch back later
            _oldNsFd = File.OpenHandle(NetNsSelfPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Open a handle to the target netns
            using var targetFd = File.OpenHandle(Path.Combine(NetNsBasePath, name), FileMode.Open, FileAccess.Read, FileShare.Read);

            // Switch this thread to the target netns
            if (LibC.setns(targetFd.DangerousGetHandle().ToInt32(), LibC.CLONE_NEWNET) < 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        public void Dispose()
        {
            if (LibC.setns(_oldNsFd.DangerousGetHandle().ToInt32(), LibC.CLONE_NEWNET) < 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            _oldNsFd.Dispose();
        }
    }
}