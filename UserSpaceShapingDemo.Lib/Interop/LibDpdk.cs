using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Interop;
internal static unsafe partial class LibDpdk
{
    private const string Lib = "libdpdk";
    private const string Shim = """
                                #include <rte_errno.h>
                                int get_rte_errno(void) { return rte_errno; }
                                """;

    private static readonly delegate* unmanaged[Cdecl]<int> _get_rte_errno;

    static LibDpdk()
    {
        // pkg-config --cflags --libs libdpdk
        var process = new Process
        {
            StartInfo =
            {
                FileName = "pkg-config",
                Arguments = "--cflags --libs libdpdk",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.Start();
        var options = process.StandardOutput
                             .ReadToEnd()
                             .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             .Where(o => o != "-lbsd") // Fails to link on some systems
                             .ToList();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"pkg-config failed with exit code {process.ExitCode}: {error}");

        // cc -O2 -fPIC -shared -o {SoPath} -x c - "$(pkg-config --cflags --libs libdpdk) -Wl,--no-as-needed"
        var soPath = Path.GetTempFileName();
        try
        {
            var ccProcess = new Process
            {
                StartInfo =
                {
                    FileName = "cc",
                    Arguments = $"-O2 -fPIC -shared -o {soPath} -x c - {string.Join(' ', options)} -Wl,--no-as-needed",
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                }
            };
            ccProcess.Start();
            ccProcess.StandardInput.Write(Shim);
            ccProcess.StandardInput.Close();
            var ccError = ccProcess.StandardError.ReadToEnd();
            ccProcess.WaitForExit();
            if (ccProcess.ExitCode != 0)
                throw new InvalidOperationException($"cc failed with exit code {ccProcess.ExitCode}: {ccError}");
            var shimLib = NativeLibrary.Load(soPath);
            _get_rte_errno = (delegate* unmanaged[Cdecl]<int>)NativeLibrary.GetExport(shimLib, "get_rte_errno");
        }
        finally
        {
            File.Delete(soPath);
        }
    }

    public static int rte_errno
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _get_rte_errno();
    }

    [LibraryImport(Lib, EntryPoint = "rte_eal_init")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial int rte_eal_init(int argc, byte** argv);
}