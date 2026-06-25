namespace NvForge.Core.Tweaks;

/// <summary>
/// Built-in Windows performance tweaks for GPU/gaming. All are reversible (a
/// backup is captured before applying). Several require a reboot to take full
/// effect; that is flagged on each definition.
/// </summary>
public static class WindowsTweakCatalog
{
    public const string HagsId = "hags";
    public const string UltimatePerformanceId = "ultimate-performance";
    public const string TdrDelayId = "tdr-delay";
    public const string FullscreenOptId = "disable-fso";
    public const string GameDvrId = "disable-gamedvr";

    /// <summary>Ultimate Performance power-scheme template GUID (Windows built-in).</summary>
    public const string UltimatePerformanceTemplateGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private const string GraphicsDriversKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";

    public static IReadOnlyList<TweakDefinition> All() => new[]
    {
        Hags(),
        UltimatePerformance(),
        TdrDelay(),
        DisableFullscreenOptimizations(),
        DisableGameDvr(),
    };

    public static TweakDefinition Hags() => new()
    {
        Id = HagsId,
        Name = "Hardware-Accelerated GPU Scheduling (HAGS)",
        Category = "Performance",
        Description = "Lets the GPU manage its own memory scheduling; can reduce latency. Requires a reboot.",
        RequiresReboot = true,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.LocalMachine,
                Path = GraphicsDriversKey,
                ValueName = "HwSchMode",
                ValueType = RegistryValueType.Dword,
                DesiredData = "2", // 2 = enabled, 1 = disabled
            },
        },
    };

    public static TweakDefinition UltimatePerformance() => new()
    {
        Id = UltimatePerformanceId,
        Name = "Ultimate Performance power plan",
        Category = "Performance",
        Description = "Creates and activates the hidden Ultimate Performance power plan (no power throttling).",
        RequiresReboot = false,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.PowerScheme,
                Path = UltimatePerformanceTemplateGuid,
                DesiredEnabled = true,
            },
        },
    };

    public static TweakDefinition TdrDelay() => new()
    {
        Id = TdrDelayId,
        Name = "Increase TDR delay",
        Category = "Stability",
        Description = "Raises the GPU timeout-detection delay to 10s to avoid driver resets under heavy load. Requires a reboot.",
        RequiresReboot = true,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.LocalMachine,
                Path = GraphicsDriversKey,
                ValueName = "TdrDelay",
                ValueType = RegistryValueType.Dword,
                DesiredData = "10",
            },
        },
    };

    public static TweakDefinition DisableFullscreenOptimizations() => new()
    {
        Id = FullscreenOptId,
        Name = "Disable Fullscreen Optimizations (global)",
        Category = "Gaming",
        Description = "Prefers true exclusive fullscreen, which can lower latency in many games.",
        RequiresReboot = false,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.CurrentUser,
                Path = @"System\GameConfigStore",
                ValueName = "GameDVR_FSEBehaviorMode",
                ValueType = RegistryValueType.Dword,
                DesiredData = "2",
            },
        },
    };

    public static TweakDefinition DisableGameDvr() => new()
    {
        Id = GameDvrId,
        Name = "Disable Game DVR / Game Bar capture",
        Category = "Gaming",
        Description = "Turns off background game recording, freeing GPU/CPU overhead.",
        RequiresReboot = false,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.CurrentUser,
                Path = @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                ValueName = "AppCaptureEnabled",
                ValueType = RegistryValueType.Dword,
                DesiredData = "0",
            },
            new TweakOperation
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.LocalMachine,
                Path = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
                ValueName = "AllowGameDVR",
                ValueType = RegistryValueType.Dword,
                DesiredData = "0",
            },
        },
    };

    /// <summary>
    /// Builds an MSI-mode (message-signaled interrupts) tweak for a specific GPU,
    /// keyed off its PnP device id. Often improves latency/responsiveness.
    /// Requires a reboot.
    /// </summary>
    public static TweakDefinition MsiModeForGpu(string pnpDeviceId) => new()
    {
        Id = $"msi-mode::{pnpDeviceId}",
        Name = "Enable MSI mode for the GPU",
        Category = "Performance",
        Description = "Switches the GPU to message-signaled interrupts (MSI). Can reduce latency. Requires a reboot.",
        RequiresReboot = true,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.LocalMachine,
                Path = $@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties",
                ValueName = "MSISupported",
                ValueType = RegistryValueType.Dword,
                DesiredData = "1",
            },
        },
    };
}
