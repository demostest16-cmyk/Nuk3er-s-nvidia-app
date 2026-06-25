using System.Runtime.InteropServices;
using System.Text;
using NvForge.Core.Models;

namespace NvForge.App.Services;

/// <summary>
/// Builds the plain-text diagnostics bundle the user can copy/paste back for
/// remote debugging (since the developer has no access to the hardware).
/// </summary>
public static class DiagnosticsReportBuilder
{
    public static string Build(
        IReadOnlyList<GpuInfo> gpus,
        string source,
        bool elevated,
        bool simulated,
        string appVersion,
        DateTime timestampUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== NvForge diagnostics ===");
        sb.AppendLine($"App version : {appVersion}");
        sb.AppendLine($"Generated   : {timestampUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"OS          : {RuntimeInformation.OSDescription}");
        sb.AppendLine($"Architecture: {RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"Elevated    : {(elevated ? "yes" : "NO (some actions will fail)")}");
        sb.AppendLine($"Mode        : {(simulated ? "SIMULATE (mock data)" : "live")}");
        sb.AppendLine($"GPU source  : {source}");
        sb.AppendLine($"GPUs found  : {gpus.Count}");
        sb.AppendLine();

        if (gpus.Count == 0)
        {
            sb.AppendLine("No NVIDIA GPUs were detected.");
            return sb.ToString();
        }

        var i = 0;
        foreach (var gpu in gpus)
        {
            sb.AppendLine($"[GPU {i++}] {gpu.Name}");
            sb.AppendLine($"  Form factor : {gpu.FormFactor}");
            sb.AppendLine($"  VRAM        : {(gpu.VramBytes > 0 ? $"{gpu.VramGigabytes:0.#} GB" : "unknown")}");
            sb.AppendLine($"  Driver      : {(string.IsNullOrEmpty(gpu.DriverVersion) ? "unknown" : gpu.DriverVersion)}");
            sb.AppendLine($"  PnP id      : {gpu.PnpDeviceId}");
            sb.AppendLine($"  Vendor lock : {(gpu.IsVendorLocked ? "likely (mobile)" : "no")}");
            sb.AppendLine($"  Capabilities: {gpu.Capabilities}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
