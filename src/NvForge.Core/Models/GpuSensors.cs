namespace NvForge.Core.Models;

/// <summary>
/// A single live-sensor reading for one GPU. Every field is nullable because not
/// every part/driver exposes every sensor (mobile parts in particular).
/// </summary>
public sealed class GpuSensors
{
    public DateTime TimestampUtc { get; init; }

    public int? CoreClockMhz { get; init; }
    public int? MemoryClockMhz { get; init; }
    public int? TemperatureC { get; init; }
    public int? FanPercent { get; init; }
    public double? PowerWatts { get; init; }
    public double? PowerLimitWatts { get; init; }
    public int? GpuUtilPercent { get; init; }
    public int? MemUtilPercent { get; init; }
    public long? VramUsedBytes { get; init; }
    public long? VramTotalBytes { get; init; }

    public double? VramUsedGb => VramUsedBytes is { } b ? b / (1024d * 1024 * 1024) : null;
    public double? VramTotalGb => VramTotalBytes is { } b ? b / (1024d * 1024 * 1024) : null;

    public double? PowerPercent =>
        PowerWatts is { } p && PowerLimitWatts is { } l && l > 0 ? 100.0 * p / l : null;
}
