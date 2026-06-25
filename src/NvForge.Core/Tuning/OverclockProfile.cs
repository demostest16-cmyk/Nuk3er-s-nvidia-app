namespace NvForge.Core.Tuning;

/// <summary>A saved overclock preset (core/memory offsets in MHz).</summary>
public sealed class OverclockProfile
{
    public string Name { get; init; } = string.Empty;
    public int CoreOffsetMhz { get; init; }
    public int MemoryOffsetMhz { get; init; }
}
