namespace NvForge.Core.Models;

/// <summary>
/// Current overclock state for one GPU as reported by the tuning backend.
/// <c>*Editable</c> reflects whether the driver/part allows adjusting that
/// domain — typically false for vendor-locked mobile parts.
/// </summary>
public sealed class TuningState
{
    public bool CoreEditable { get; init; }
    public int CoreOffsetMhz { get; init; }
    public int CoreMinMhz { get; init; }
    public int CoreMaxMhz { get; init; }

    public bool MemoryEditable { get; init; }
    public int MemoryOffsetMhz { get; init; }
    public int MemoryMinMhz { get; init; }
    public int MemoryMaxMhz { get; init; }

    public bool AnyEditable => CoreEditable || MemoryEditable;
}
