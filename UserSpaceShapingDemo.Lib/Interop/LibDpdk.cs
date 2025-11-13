using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Interop;

internal static unsafe class LibDpdk
{
    private const string Shim = """
                                #include <rte_errno.h>
                                int get_rte_errno(void) { return rte_errno; }
                                """;

    private static readonly delegate* unmanaged[Cdecl]<NativeErrorNumber> _get_rte_errno;

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
            
            _get_rte_errno = (delegate* unmanaged[Cdecl]<NativeErrorNumber>)NativeLibrary.GetExport(shimLib, "get_rte_errno");
            rte_strerror = (delegate* unmanaged[Cdecl]<NativeErrorNumber, byte*>)NativeLibrary.GetExport(shimLib, "rte_strerror");
            rte_eal_init = (delegate* unmanaged[Cdecl]<int, byte**, int>)NativeLibrary.GetExport(shimLib, "rte_eal_init");
        }
        finally
        {
            File.Delete(soPath);
        }
    }

    public static NativeErrorNumber rte_errno
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _get_rte_errno();
    }

    // const char *rte_strerror(int errnum);
    public static readonly delegate* unmanaged[Cdecl]<NativeErrorNumber, byte*> rte_strerror;

    // int rte_eal_init(int argc, char **argv)
    public static readonly delegate* unmanaged[Cdecl]<int, byte**, int> rte_eal_init;
}