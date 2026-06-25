namespace NvForge.Core.Tweaks;

/// <summary>
/// A snapshot of the system state a tweak touched, captured immediately before
/// the tweak was applied. Persisted as JSON by <see cref="TweakBackupStore"/>
/// and consumed by the restore path to revert the change exactly.
/// </summary>
public sealed class TweakBackup
{
    public string TweakId { get; init; } = string.Empty;

    /// <summary>When the backup was taken (UTC). Set by the caller for testability.</summary>
    public DateTime TimestampUtc { get; init; }

    /// <summary>Human-readable tweak name at the time of capture.</summary>
    public string TweakName { get; init; } = string.Empty;

    public List<TweakOperationBackup> Operations { get; init; } = new();
}
