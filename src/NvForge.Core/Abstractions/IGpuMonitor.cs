using NvForge.Core.Models;

namespace NvForge.Core.Abstractions;

/// <summary>A GPU as seen by a live monitor (NVML or the mock).</summary>
public sealed class MonitoredGpu
{
    public required int Index { get; init; }
    public required string Name { get; init; }
}

/// <summary>
/// Source of live GPU sensor data. Implemented by the NVML monitor (real
/// hardware) and the mock monitor (<c>--simulate</c> / no GPU). Initialization
/// happens in the implementation's constructor; check <see cref="Available"/>
/// before reading.
/// </summary>
public interface IGpuMonitor : IDisposable
{
    string SourceName { get; }

    /// <summary>True when the monitor initialized and at least one device is present.</summary>
    bool Available { get; }

    /// <summary>Why the monitor is unavailable (e.g. driver not installed), if applicable.</summary>
    string? UnavailableReason { get; }

    IReadOnlyList<MonitoredGpu> Devices { get; }

    /// <summary>Reads a fresh snapshot for the device at <paramref name="index"/>.</summary>
    GpuSensors ReadSensors(int index);
}
