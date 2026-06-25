using NvForge.Core.Abstractions;
using NvForge.Core.Models;

namespace NvForge.Core.Tuning;

/// <summary>
/// In-memory tuner for <c>--simulate</c>: the desktop part accepts offsets, the
/// laptop part is reported locked (mirroring typical mobile vBIOS behavior), so
/// the capability-gated UI can be exercised without hardware.
/// </summary>
public sealed class MockGpuTuner : IGpuTuner
{
    private readonly List<MonitoredGpu> _devices = new()
    {
        new() { Index = 0, Name = "NVIDIA GeForce RTX 4060" },
        new() { Index = 1, Name = "NVIDIA GeForce RTX 5070 Ti Laptop GPU" },
    };

    private readonly Dictionary<int, (int core, int mem)> _offsets = new()
    {
        [0] = (0, 0),
        [1] = (0, 0),
    };

    public string SourceName => "Simulated tuner (mock)";
    public bool Available => true;
    public string? UnavailableReason => null;
    public IReadOnlyList<MonitoredGpu> Devices => _devices;

    public TuningState ReadState(int index)
    {
        var locked = index == 1; // the laptop part
        var (core, mem) = _offsets.TryGetValue(index, out var o) ? o : (0, 0);
        return new TuningState
        {
            CoreEditable = !locked,
            CoreOffsetMhz = core,
            CoreMinMhz = -200,
            CoreMaxMhz = 350,
            MemoryEditable = !locked,
            MemoryOffsetMhz = mem,
            MemoryMinMhz = -500,
            MemoryMaxMhz = 2000,
        };
    }

    public TuningApplyResult ApplyOffsets(int index, int coreOffsetMhz, int memoryOffsetMhz)
    {
        if (index == 1)
            return new TuningApplyResult { Success = false, Message = "This laptop GPU is locked by the vendor (simulated)." };

        _offsets[index] = (coreOffsetMhz, memoryOffsetMhz);
        return new TuningApplyResult { Success = true, Message = $"Applied core {coreOffsetMhz:+#;-#;0} MHz, memory {memoryOffsetMhz:+#;-#;0} MHz (simulated)." };
    }

    public void Dispose() { }
}
