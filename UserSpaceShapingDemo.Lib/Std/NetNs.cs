using System;
using System.IO;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

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
        using var oldNs = OpenCurrent();

        var target = Path.Combine(NetNsBasePath, name);
        try
        {
            // Create the target file (regular file is fine) that we'll bind-mount onto
            using (new NativeFile(target, NativeFileFlags.Create | NativeFileFlags.Exclusive | NativeFileFlags.ReadWrite, NetNsFileMode)) { }

            // Create a new network namespace for *this thread*
            LibC.unshare(LibC.CLONE_NEWNET).ThrowIfError();
            try
            {
                // Open a handle to the *new* netns we just created
                using var ns = OpenCurrent();

                // Bind-mount the new netns to /run/netns/<name> to persist it.
                // Using the /proc/thread-self/fd/<nsFd> path ensures we bind the FD we opened.
                var srcPath = Path.Combine(SelfThreadFdPath, ns.Descriptor.ToString());
                LibC.mount(srcPath, target, null, LibC.MS_BIND, null).ThrowIfError();
            }
            finally
            {
                Set(oldNs);
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
        LibC.umount2(target, LibC.MNT_DETACH).ThrowIfError();
        File.Delete(target);
    }

    public static bool Exists(string name) => File.Exists(Path.Combine(NetNsBasePath, name));

    public static bool ReCreate(string name)
    {
        var existed = Exists(name);
        if (existed)
            Delete(name);
        Add(name);
        return existed;
    }

    public static string[] List() => Directory.Exists(NetNsBasePath) ? Directory.GetFiles(NetNsBasePath) : [];

    public static Scope Enter(string name) => new(Path.Combine(NetNsBasePath, name));

    public static Scope EnterRoot() => new(RootNsNetPath);

    private static NativeFile OpenPath(string path) => new(path, NativeFileFlags.ReadOnly);

    private static NativeFile OpenCurrent() => OpenPath(SelfThreadNsNetPath);

    public static NativeFile Open(string name) => OpenPath(Path.Combine(NetNsBasePath, name));

    private static void Set(NativeFile ns) => LibC.setns(ns.Descriptor, LibC.CLONE_NEWNET).ThrowIfError();

    public sealed class Scope : IDisposable
    {
        private readonly NativeFile _oldNs;

        internal Scope(string path)
        {
            _oldNs = OpenCurrent();
            using var targetNs = OpenPath(path);
            Set(targetNs);
        }

        public void Dispose()
        {
            try
            {
                Set(_oldNs);
            }
            finally
            {
                _oldNs.Dispose();
            }
        }
    }
}