using NvForge.Core.Models;

namespace NvForge.Core.Gpu;

/// <summary>Result of classifying an adapter by its name.</summary>
public readonly record struct GpuClassification(
    bool IsNvidia,
    GpuFormFactor FormFactor,
    CapabilityFlags Capabilities);

/// <summary>
/// Pure, hardware-free classification of a GPU from its adapter name. Kept in
/// Core (not the WMI provider) so it is unit-testable on any OS, including the
/// Linux CI runner.
/// </summary>
public static class GpuClassifier
{
    private static readonly string[] NvidiaMarkers =
        { "nvidia", "geforce", "rtx", "gtx", "quadro", "tesla", "titan", "nvs " };

    // Substrings that mark a mobile/laptop part. "Laptop GPU" is how RTX 30/40/50
    // mobile parts are branded (e.g. "NVIDIA GeForce RTX 5070 Ti Laptop GPU").
    private static readonly string[] MobileMarkers =
        { "laptop gpu", "laptop", "mobile", "max-q", "max q", " m " };

    public static GpuClassification Classify(string? adapterName)
    {
        var name = (adapterName ?? string.Empty).Trim();
        var lower = name.ToLowerInvariant();

        var isNvidia = NvidiaMarkers.Any(m => lower.Contains(m));
        if (!isNvidia)
            return new GpuClassification(false, GpuFormFactor.Unknown, CapabilityFlags.None);

        var isMobile = MobileMarkers.Any(m => lower.Contains(m));
        var formFactor = isMobile ? GpuFormFactor.Mobile : GpuFormFactor.Desktop;

        return new GpuClassification(true, formFactor, CapabilitiesFor(formFactor));
    }

    /// <summary>
    /// Conservative, forward-looking capability expectation by form factor.
    /// Desktop parts expose full tuning; mobile parts are assumed locked for
    /// power/thermal/voltage (typical for vendor vBIOS) until a later phase
    /// proves otherwise at runtime.
    /// </summary>
    public static CapabilityFlags CapabilitiesFor(GpuFormFactor formFactor) => formFactor switch
    {
        GpuFormFactor.Desktop =>
            CapabilityFlags.Monitoring
            | CapabilityFlags.CoreClockOffset
            | CapabilityFlags.MemoryClockOffset
            | CapabilityFlags.PowerLimit
            | CapabilityFlags.TempLimit
            | CapabilityFlags.FanControl
            | CapabilityFlags.VoltageControl
            | CapabilityFlags.DriverCustomization,

        GpuFormFactor.Mobile =>
            CapabilityFlags.Monitoring
            | CapabilityFlags.CoreClockOffset
            | CapabilityFlags.MemoryClockOffset
            | CapabilityFlags.DriverCustomization,

        _ => CapabilityFlags.None,
    };
}
