using NvForge.Core.Models;

namespace NvForge.Core.Abstractions;

/// <summary>
/// Supplies the set of GPUs present on the system. Implemented by the
/// Windows-only WMI provider (real hardware) and by the in-Core mock provider
/// (used for <c>--simulate</c> and unit tests on a machine with no GPU).
/// </summary>
public interface IGpuInfoProvider
{
    /// <summary>The provider's display name (for diagnostics).</summary>
    string SourceName { get; }

    /// <summary>Returns all detected adapters. Never null; may be empty.</summary>
    IReadOnlyList<GpuInfo> GetGpus();
}
