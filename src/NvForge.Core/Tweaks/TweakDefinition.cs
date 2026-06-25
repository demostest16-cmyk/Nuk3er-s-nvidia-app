namespace NvForge.Core.Tweaks;

/// <summary>
/// A named, reversible system change made up of one or more
/// <see cref="TweakOperation"/>s. Definitions are pure data; a Windows service
/// (in NvForge.DriverCustomizer) executes them and records a
/// <see cref="TweakBackup"/> first.
/// </summary>
public sealed class TweakDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }

    /// <summary>True when the change only fully takes effect after a reboot.</summary>
    public bool RequiresReboot { get; init; }

    public required IReadOnlyList<TweakOperation> Operations { get; init; }
}
