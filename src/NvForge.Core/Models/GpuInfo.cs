namespace NvForge.Core.Models;

/// <summary>
/// Static description of a detected GPU. Live sensor values (clocks, temps,
/// fan, power) are a Phase 2 concern (NVML) and are intentionally not here.
/// </summary>
public sealed class GpuInfo
{
    /// <summary>Zero-based index in the system's adapter list.</summary>
    public int Index { get; init; }

    /// <summary>Adapter name, e.g. "NVIDIA GeForce RTX 4060".</summary>
    public string Name { get; init; } = "Unknown GPU";

    /// <summary>True when this is an NVIDIA adapter (the only kind NvForge manages).</summary>
    public bool IsNvidia { get; init; }

    public GpuFormFactor FormFactor { get; init; } = GpuFormFactor.Unknown;

    /// <summary>Dedicated video memory in bytes (0 if unknown).</summary>
    public long VramBytes { get; init; }

    /// <summary>Installed display-driver version string (empty if unknown).</summary>
    public string DriverVersion { get; init; } = string.Empty;

    /// <summary>PnP device id, used as a stable key and for registry lookups.</summary>
    public string PnpDeviceId { get; init; } = string.Empty;

    /// <summary>Expected tuning capabilities for this part.</summary>
    public CapabilityFlags Capabilities { get; init; } = CapabilityFlags.None;

    /// <summary>True when the form factor suggests vendor-locked tuning (mobile).</summary>
    public bool IsVendorLocked => FormFactor == GpuFormFactor.Mobile;

    public double VramGigabytes => VramBytes / (1024d * 1024d * 1024d);

    public bool Supports(CapabilityFlags capability) => (Capabilities & capability) == capability;

    public override string ToString() =>
        $"{Name} ({FormFactor}, {VramGigabytes:0.#} GB, driver {(string.IsNullOrEmpty(DriverVersion) ? "?" : DriverVersion)})";
}
