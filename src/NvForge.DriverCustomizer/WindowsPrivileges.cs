using System.Runtime.Versioning;
using System.Security.Principal;

namespace NvForge.DriverCustomizer;

/// <summary>Helpers for checking the process's privilege level.</summary>
[SupportedOSPlatform("windows")]
public static class WindowsPrivileges
{
    /// <summary>True when the current process is running with an elevated (Administrator) token.</summary>
    public static bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
