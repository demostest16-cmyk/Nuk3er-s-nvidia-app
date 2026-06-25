namespace NvForge.Core.Tweaks;

/// <summary>
/// The built-in set of reversible privacy/update tweaks NvForge can apply after
/// a driver install. These are intentionally conservative and best-effort:
/// scheduled-task and service operations that match nothing on the system are
/// simply recorded as "did not exist" and skipped.
/// </summary>
public static class TweakCatalog
{
    /// <summary>Stable id of the telemetry tweak.</summary>
    public const string DisableTelemetryId = "disable-nv-telemetry";

    /// <summary>Stable id of the driver update-check tweak.</summary>
    public const string DisableUpdateChecksId = "disable-nv-update-checks";

    public static IReadOnlyList<TweakDefinition> All() => new[]
    {
        DisableTelemetry(),
        DisableUpdateChecks(),
    };

    public static TweakDefinition DisableTelemetry() => new()
    {
        Id = DisableTelemetryId,
        Name = "Disable NVIDIA telemetry",
        Category = "Privacy",
        Description =
            "Opts out of telemetry, disables the NVIDIA telemetry scheduled tasks " +
            "(NvTm*) and the NvTelemetryContainer service.",
        RequiresReboot = false,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.LocalMachine,
                Path = @"SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client",
                ValueName = "OptInOrOutPreference",
                ValueType = RegistryValueType.Dword,
                DesiredData = "0",
            },
            new TweakOperation
            {
                Kind = TweakOperationKind.ScheduledTask,
                Path = "NvTm",          // matches NvTmRep_*, NvTmMon_*, NvTmRepOnLogon_*
                DesiredEnabled = false,
            },
            new TweakOperation
            {
                Kind = TweakOperationKind.Service,
                Path = "NvTelemetryContainer",
                DesiredEnabled = false,
            },
        },
    };

    public static TweakDefinition DisableUpdateChecks() => new()
    {
        Id = DisableUpdateChecksId,
        Name = "Disable driver auto-update checks",
        Category = "Updates",
        Description =
            "Disables the NVIDIA profile/driver update scheduled tasks so the driver " +
            "does not nag or auto-update in the background.",
        RequiresReboot = false,
        Operations = new[]
        {
            new TweakOperation
            {
                Kind = TweakOperationKind.ScheduledTask,
                Path = "NvProfileUpdater",   // NvProfileUpdaterDaily_*, NvProfileUpdaterOnLogon_*
                DesiredEnabled = false,
            },
            new TweakOperation
            {
                Kind = TweakOperationKind.ScheduledTask,
                Path = "NvDriverUpdate",
                DesiredEnabled = false,
            },
        },
    };
}
