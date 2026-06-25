namespace NvForge.Core.Abstractions;

/// <summary>Outcome of writing a driver setting.</summary>
public sealed class DriverSettingsResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Reads and writes global NVIDIA Control Panel (driver) settings via NVAPI DRS.
/// Settings apply to the base profile (system-wide), so there is no per-GPU
/// index. Implemented by the NVAPI service (real hardware) and a mock
/// (<c>--simulate</c>).
/// </summary>
public interface IDriverSettingsService : IDisposable
{
    string SourceName { get; }

    bool Available { get; }

    string? UnavailableReason { get; }

    /// <summary>Reads the current DWORD value of a setting, or null if unset/not found.</summary>
    uint? Read(uint settingId);

    /// <summary>Writes a DWORD setting to the base profile and saves.</summary>
    DriverSettingsResult Write(uint settingId, uint value);
}
