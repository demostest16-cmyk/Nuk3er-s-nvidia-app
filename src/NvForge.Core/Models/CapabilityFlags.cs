namespace NvForge.Core.Models;

/// <summary>
/// What a given GPU is expected to allow. These gate the UI so controls that a
/// part cannot honor (e.g. power-limit / voltage on a vendor-locked laptop GPU)
/// are hidden or disabled rather than silently doing nothing.
///
/// Flags are an initial, conservative expectation derived from the form factor;
/// later phases (NVML/NVAPI) will refine them against what the driver actually
/// reports at runtime.
/// </summary>
[Flags]
public enum CapabilityFlags
{
    None = 0,

    /// <summary>Read-only sensors: clocks, temps, fan, power, utilization.</summary>
    Monitoring = 1 << 0,

    /// <summary>Core clock offset (MHz).</summary>
    CoreClockOffset = 1 << 1,

    /// <summary>Memory clock offset (MHz).</summary>
    MemoryClockOffset = 1 << 2,

    /// <summary>Adjustable power limit (%). Usually locked on mobile parts.</summary>
    PowerLimit = 1 << 3,

    /// <summary>Adjustable thermal limit (°C). Usually locked on mobile parts.</summary>
    TempLimit = 1 << 4,

    /// <summary>Manual fan curve / fan speed control.</summary>
    FanControl = 1 << 5,

    /// <summary>Core voltage / voltage-boost control. Usually locked on mobile parts.</summary>
    VoltageControl = 1 << 6,

    /// <summary>Driver customization (NVCleanstall orchestration, telemetry tweaks).</summary>
    DriverCustomization = 1 << 7,
}
