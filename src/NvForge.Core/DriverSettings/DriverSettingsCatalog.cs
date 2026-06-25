using NvForge.Core.Models;

namespace NvForge.Core.DriverSettings;

/// <summary>
/// The NVIDIA Control Panel settings NvForge can toggle. Setting ids and value
/// constants are the public NVAPI driver-settings (DRS) values. Each entry pairs
/// a performance/latency-oriented value with NVIDIA's default for one-click revert.
/// </summary>
public static class DriverSettingsCatalog
{
    // --- NVAPI DRS setting ids ---
    public const uint PreferredPstateId = 0x1057EB71; // Power management mode
    public const uint PrerenderLimitId = 0x007BA09E;  // Max pre-rendered frames (low latency)
    public const uint VsyncModeId = 0x66B435D8;        // Vertical sync
    public const uint OglThreadControlId = 0x20FF7493; // Threaded optimization (OpenGL)

    // --- value constants ---
    public const uint PreferMaxPerformance = 0x00000001;
    public const uint PstateAdaptive = 0x00000000;

    public const uint VsyncForceOff = 0x08416747;
    public const uint VsyncDefault = 0x60925292; // VSYNCMODE_PASSIVE (use the 3D app setting)

    public const uint OglThreadEnable = 0x00000001;
    public const uint OglThreadAuto = 0x00000000;

    public static IReadOnlyList<DriverSetting> All() => new[]
    {
        new DriverSetting
        {
            Id = "power-management-mode",
            NvcpName = "Power management mode",
            Description = "Prefer Maximum Performance keeps clocks high instead of down-clocking at idle.",
            SettingId = PreferredPstateId,
            OptimizedValue = PreferMaxPerformance,
            DefaultValue = PstateAdaptive,
        },
        new DriverSetting
        {
            Id = "low-latency",
            NvcpName = "Low Latency (max pre-rendered frames = 1)",
            Description = "Limits queued frames to 1 to reduce input latency.",
            SettingId = PrerenderLimitId,
            OptimizedValue = 1,
            DefaultValue = 0, // 0 = application-controlled (default)
        },
        new DriverSetting
        {
            Id = "vertical-sync",
            NvcpName = "Vertical sync",
            Description = "Forces V-Sync off for lower latency / uncapped frame rates.",
            SettingId = VsyncModeId,
            OptimizedValue = VsyncForceOff,
            DefaultValue = VsyncDefault,
        },
        new DriverSetting
        {
            Id = "threaded-optimization",
            NvcpName = "Threaded optimization",
            Description = "Enables multi-threaded driver work (helps CPU-bound OpenGL titles).",
            SettingId = OglThreadControlId,
            OptimizedValue = OglThreadEnable,
            DefaultValue = OglThreadAuto,
        },
    };
}
