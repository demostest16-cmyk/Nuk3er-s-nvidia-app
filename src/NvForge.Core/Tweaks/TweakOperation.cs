namespace NvForge.Core.Tweaks;

/// <summary>
/// One atomic change a tweak applies. A single flat shape (with a
/// <see cref="Kind"/> discriminator) is used instead of a class hierarchy so it
/// serializes to/from JSON cleanly with System.Text.Json.
///
/// Field meaning by kind:
/// <list type="bullet">
/// <item><b>RegistryValue</b>: <see cref="Hive"/> + <see cref="Path"/> (subkey) +
/// <see cref="ValueName"/> + <see cref="ValueType"/> + <see cref="DesiredData"/>.</item>
/// <item><b>ScheduledTask</b>: <see cref="Path"/> is a task-name <i>prefix</i>; all
/// matching tasks are set to <see cref="DesiredEnabled"/>.</item>
/// <item><b>Service</b>: <see cref="Path"/> is the service name; it is disabled
/// when <see cref="DesiredEnabled"/> is false.</item>
/// </list>
/// </summary>
public sealed class TweakOperation
{
    public TweakOperationKind Kind { get; init; }

    public RegistryHive Hive { get; init; }

    /// <summary>Registry subkey, scheduled-task name prefix, or service name.</summary>
    public string Path { get; init; } = string.Empty;

    public string? ValueName { get; init; }

    public RegistryValueType ValueType { get; init; }

    /// <summary>Desired registry value, encoded as a string (e.g. "0" for a Dword).</summary>
    public string? DesiredData { get; init; }

    /// <summary>For ScheduledTask/Service: the desired enabled state (false = disable).</summary>
    public bool DesiredEnabled { get; init; }
}

/// <summary>
/// Captured prior state for one operation, written before the tweak is applied
/// so the change can be reverted exactly.
/// </summary>
public sealed class TweakOperationBackup
{
    public TweakOperationKind Kind { get; init; }
    public RegistryHive Hive { get; init; }
    public string Path { get; init; } = string.Empty;
    public string? ValueName { get; init; }

    /// <summary>Whether the value/task/service existed before the tweak ran.</summary>
    public bool Existed { get; init; }

    public RegistryValueType ValueType { get; init; }

    /// <summary>
    /// Original data. For RegistryValue: the prior value (string-encoded). For
    /// ScheduledTask/Service: prior enabled state as "true"/"false". For Service
    /// this also stores the prior start mode after a '|' separator when known.
    /// </summary>
    public string? OriginalData { get; init; }
}
