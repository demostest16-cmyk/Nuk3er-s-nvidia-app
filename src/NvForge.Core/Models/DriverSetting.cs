namespace NvForge.Core.Models;

/// <summary>
/// A single NVIDIA Control Panel (driver) setting NvForge can toggle via NVAPI's
/// Driver Settings (DRS) API. <see cref="OptimizedValue"/> is the performance/
/// latency-oriented value; <see cref="DefaultValue"/> is NVIDIA's default, used
/// by "Restore". Both are the raw DWORD constants from NVAPI's driver-settings
/// header.
/// </summary>
public sealed class DriverSetting
{
    public required string Id { get; init; }

    /// <summary>The label as it appears in the NVIDIA Control Panel.</summary>
    public required string NvcpName { get; init; }

    public required string Description { get; init; }

    /// <summary>NVAPI DRS setting id (e.g. PREFERRED_PSTATE_ID).</summary>
    public required uint SettingId { get; init; }

    public required uint OptimizedValue { get; init; }

    public required uint DefaultValue { get; init; }
}
