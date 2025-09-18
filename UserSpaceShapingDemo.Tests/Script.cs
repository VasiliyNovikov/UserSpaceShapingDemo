using System;
using System.Diagnostics;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UserSpaceShapingDemo.Tests;

internal static class Script
{
    private static string Exec(ReadOnlySpan<string> cmd, bool throwOnError)
    {
        using var process = new Process
        {
            StartInfo =
            {
                FileName = cmd[0],
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        foreach (var arg in cmd[1..])
            process.StartInfo.ArgumentList.Add(arg);

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => HandleDataReceived(output, e);
        var error = new StringBuilder();
        process.ErrorDataReceived += (_, e) => HandleDataReceived(error, e);

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        return throwOnError && process.ExitCode != 0
            ? throw new AssertFailedException($"Command '{string.Join(" ", cmd)}' failed with exit code {process.ExitCode}: {error.ToString().Trim()}")
            : output.ToString().Trim();

        static void HandleDataReceived(StringBuilder target, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;
            if (target.Length > 0)
                target.Append('\n');
            target.Append(e.Data);
        }
    }

    public static string Exec(params ReadOnlySpan<string> cmd) => Exec(cmd, true);
    public static string ExecNoThrow(params ReadOnlySpan<string> cmd) => Exec(cmd, false);
    public static string[] ExecLines(params ReadOnlySpan<string> cmd) => Exec(cmd).Split('\n');
}