namespace NvForge.Core.Models;

/// <summary>
/// Physical form factor of a GPU. Drives capability gating: mobile parts are
/// frequently locked by the laptop vendor's vBIOS (power/voltage limits, fan
/// control), so the UI must not assume desktop-class control on them.
/// </summary>
public enum GpuFormFactor
{
    Unknown = 0,
    Desktop = 1,
    Mobile = 2,
}
