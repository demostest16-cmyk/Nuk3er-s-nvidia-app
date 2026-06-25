namespace NvForge.Core.Drivers;

/// <summary>Suggested handling of an optional driver component during a clean install.</summary>
public enum ComponentAction
{
    /// <summary>Keep this component (needed or recommended).</summary>
    Keep = 0,

    /// <summary>Strip this component (telemetry / bloat / optional).</summary>
    Strip = 1,
}

/// <summary>
/// A curated recommendation about one optional component in an NVIDIA driver
/// package, surfaced as guidance before the user runs NVCleanstall. NvForge
/// does not edit NVIDIA's package itself in Phase 1 — this is advice plus the
/// post-install telemetry tweaks that NvForge can apply directly.
/// </summary>
public sealed class DriverComponentRecommendation
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required ComponentAction Recommended { get; init; }

    /// <summary>Short rationale shown next to the recommendation.</summary>
    public required string Reason { get; init; }
}
