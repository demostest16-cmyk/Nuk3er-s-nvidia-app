using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace NvForge.DriverCustomizer;

/// <summary>Result of running a console process.</summary>
public readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Thin wrapper around launching console utilities (schtasks.exe, sc.exe) and
/// capturing their output. Centralized so the tweak controllers stay readable.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ProcessRunner
{
    public static ProcessResult Run(string fileName, params string[] arguments)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        if (process is null)
            return new ProcessResult(-1, string.Empty, $"Failed to start {fileName}");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }
}
