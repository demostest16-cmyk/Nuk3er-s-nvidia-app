using NvForge.Core.Abstractions;
using NvForge.Core.Models;

namespace NvForge.Core.Gpu;

/// <summary>
/// Hardware-free provider used by the <c>--simulate</c> flag and unit tests.
/// Mirrors the owner's actual fleet: an RTX 4060 desktop and an RTX 5070 Ti
/// laptop GPU, so the UI (and capability gating) can be exercised on a machine
/// with no NVIDIA GPU, including CI.
/// </summary>
public sealed class MockGpuInfoProvider : IGpuInfoProvider
{
    public string SourceName => "Simulated (mock)";

    public IReadOnlyList<GpuInfo> GetGpus() => new[]
    {
        Build(0, "NVIDIA GeForce RTX 4060", 8L * 1024 * 1024 * 1024, "576.52"),
        Build(1, "NVIDIA GeForce RTX 5070 Ti Laptop GPU", 12L * 1024 * 1024 * 1024, "576.52"),
    };

    private static GpuInfo Build(int index, string name, long vram, string driver)
    {
        var c = GpuClassifier.Classify(name);
        return new GpuInfo
        {
            Index = index,
            Name = name,
            IsNvidia = c.IsNvidia,
            FormFactor = c.FormFactor,
            Capabilities = c.Capabilities,
            VramBytes = vram,
            DriverVersion = driver,
            PnpDeviceId = $"PCI\\SIMULATED_DEV_{index}",
        };
    }
}
