using System.Runtime.Versioning;
using Microsoft.Win32;

namespace NvForge.DriverCustomizer;

/// <summary>
/// Reads and changes a Windows service's start type via its registry key
/// (<c>HKLM\SYSTEM\CurrentControlSet\Services\{name}\Start</c>) and stops a
/// running service with <c>sc.exe</c>. Used to disable NvTelemetryContainer
/// reversibly. Registry start type is preferred over <c>sc config</c> because
/// it is trivially captured for backup and restored exactly.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ServiceStartupController
{
    // Win32 service start types.
    public const int StartAutomatic = 2;
    public const int StartManual = 3;
    public const int StartDisabled = 4;

    private static string KeyPath(string serviceName) =>
        $@"SYSTEM\CurrentControlSet\Services\{serviceName}";

    public bool Exists(string serviceName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath(serviceName));
        return key is not null;
    }

    public int? GetStartType(string serviceName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath(serviceName));
        return key?.GetValue("Start") is int v ? v : null;
    }

    public bool SetStartType(string serviceName, int startType)
    {
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath(serviceName), writable: true);
        if (key is null)
            return false;
        key.SetValue("Start", startType, RegistryValueKind.DWord);
        return true;
    }

    /// <summary>Best-effort stop of a running service.</summary>
    public void Stop(string serviceName) => ProcessRunner.Run("sc", "stop", serviceName);
}
