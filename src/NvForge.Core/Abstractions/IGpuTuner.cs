using NvForge.Core.Models;

namespace NvForge.Core.Abstractions;

/// <summary>Outcome of applying overclock offsets.</summary>
public sealed class TuningApplyResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Applies and reads GPU overclock offsets. Implemented by the NVAPI tuner (real
/// hardware) and the mock tuner (<c>--simulate</c>). Offsets are in MHz applied
/// on top of the stock clock curve (the standard modern OC model).
/// </summary>
public interface IGpuTuner : IDisposable
{
    string SourceName { get; }

    bool Available { get; }

    string? UnavailableReason { get; }

    IReadOnlyList<MonitoredGpu> Devices { get; }

    TuningState ReadState(int index);

    /// <summary>Applies core/memory clock offsets (MHz) to the device at <paramref name="index"/>.</summary>
    TuningApplyResult ApplyOffsets(int index, int coreOffsetMhz, int memoryOffsetMhz);
}
