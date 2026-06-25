using System.Runtime.Versioning;
using Microsoft.Win32;
using NvForge.Core.Tweaks;
using CoreHive = NvForge.Core.Tweaks.RegistryHive;
using CoreValueType = NvForge.Core.Tweaks.RegistryValueType;

namespace NvForge.DriverCustomizer;

/// <summary>Outcome of applying or restoring a tweak.</summary>
public sealed class TweakActionResult
{
    public required string TweakId { get; init; }
    public bool Success { get; init; }
    public List<string> Messages { get; init; } = new();
}

/// <summary>
/// Applies and reverts <see cref="TweakDefinition"/>s, capturing a
/// <see cref="TweakBackup"/> via <see cref="TweakBackupStore"/> before any
/// change so every tweak is one-click reversible. Handles registry values,
/// scheduled tasks, and service start types.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RegistryTweakService
{
    private readonly TweakBackupStore _store;
    private readonly ScheduledTaskController _tasks;
    private readonly ServiceStartupController _services;

    public RegistryTweakService(
        TweakBackupStore store,
        ScheduledTaskController? tasks = null,
        ServiceStartupController? services = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tasks = tasks ?? new ScheduledTaskController();
        _services = services ?? new ServiceStartupController();
    }

    /// <summary>True when NvForge has applied this tweak (a backup exists for it).</summary>
    public bool IsApplied(string tweakId) => _store.Exists(tweakId);

    public TweakActionResult Apply(TweakDefinition def)
    {
        var messages = new List<string>();
        var backup = new TweakBackup
        {
            TweakId = def.Id,
            TweakName = def.Name,
            TimestampUtc = DateTime.UtcNow,
        };

        foreach (var op in def.Operations)
        {
            try
            {
                switch (op.Kind)
                {
                    case TweakOperationKind.RegistryValue:
                        ApplyRegistry(op, backup, messages);
                        break;
                    case TweakOperationKind.ScheduledTask:
                        ApplyTasks(op, backup, messages);
                        break;
                    case TweakOperationKind.Service:
                        ApplyService(op, backup, messages);
                        break;
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Operation on '{op.Path}' failed: {ex.Message}");
            }
        }

        _store.Save(backup);
        return new TweakActionResult { TweakId = def.Id, Success = true, Messages = messages };
    }

    public TweakActionResult Restore(string tweakId)
    {
        var messages = new List<string>();
        var backup = _store.TryLoad(tweakId);
        if (backup is null)
            return new TweakActionResult { TweakId = tweakId, Success = false, Messages = { "No backup found." } };

        // Reverse order so dependent changes unwind cleanly.
        for (var i = backup.Operations.Count - 1; i >= 0; i--)
        {
            var op = backup.Operations[i];
            try
            {
                switch (op.Kind)
                {
                    case TweakOperationKind.RegistryValue:
                        RestoreRegistry(op, messages);
                        break;
                    case TweakOperationKind.ScheduledTask:
                        if (op.Existed)
                            _tasks.SetEnabled(op.Path, op.OriginalData == "true");
                        break;
                    case TweakOperationKind.Service:
                        if (op.Existed && int.TryParse(SplitOriginalStart(op.OriginalData), out var start))
                            _services.SetStartType(op.Path, start);
                        break;
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Restore of '{op.Path}' failed: {ex.Message}");
            }
        }

        _store.Delete(tweakId);
        return new TweakActionResult { TweakId = tweakId, Success = true, Messages = messages };
    }

    // ---- registry ----

    private static void ApplyRegistry(TweakOperation op, TweakBackup backup, List<string> messages)
    {
        using var baseKey = RegistryKey.OpenBaseKey(MapHive(op.Hive), RegistryView.Registry64);

        bool existed;
        string? originalData = null;
        var originalType = op.ValueType;

        using (var read = baseKey.OpenSubKey(op.Path))
        {
            var current = read?.GetValue(op.ValueName);
            existed = current is not null;
            if (existed)
            {
                originalType = MapKind(read!.GetValueKind(op.ValueName!));
                originalData = FormatValue(current!);
            }
        }

        backup.Operations.Add(new TweakOperationBackup
        {
            Kind = TweakOperationKind.RegistryValue,
            Hive = op.Hive,
            Path = op.Path,
            ValueName = op.ValueName,
            Existed = existed,
            ValueType = originalType,
            OriginalData = originalData,
        });

        using var write = baseKey.CreateSubKey(op.Path, writable: true);
        write.SetValue(op.ValueName!, ParseValue(op.DesiredData, op.ValueType), MapKind(op.ValueType));
        messages.Add($"Set {op.Path}\\{op.ValueName} = {op.DesiredData}");
    }

    private static void RestoreRegistry(TweakOperationBackup op, List<string> messages)
    {
        using var baseKey = RegistryKey.OpenBaseKey(MapHive(op.Hive), RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(op.Path, writable: true);
        if (key is null)
            return;

        if (op.Existed)
        {
            key.SetValue(op.ValueName!, ParseValue(op.OriginalData, op.ValueType), MapKind(op.ValueType));
            messages.Add($"Reverted {op.Path}\\{op.ValueName}");
        }
        else
        {
            key.DeleteValue(op.ValueName!, throwOnMissingValue: false);
            messages.Add($"Removed {op.Path}\\{op.ValueName}");
        }
    }

    // ---- scheduled tasks ----

    private void ApplyTasks(TweakOperation op, TweakBackup backup, List<string> messages)
    {
        var matches = _tasks.FindByLeafPrefix(op.Path);
        if (matches.Count == 0)
        {
            backup.Operations.Add(new TweakOperationBackup
            {
                Kind = TweakOperationKind.ScheduledTask,
                Path = op.Path,
                Existed = false,
            });
            messages.Add($"No scheduled tasks matched '{op.Path}*'.");
            return;
        }

        foreach (var task in matches)
        {
            backup.Operations.Add(new TweakOperationBackup
            {
                Kind = TweakOperationKind.ScheduledTask,
                Path = task.FullName,
                Existed = true,
                OriginalData = task.Enabled ? "true" : "false",
            });
            _tasks.SetEnabled(task.FullName, op.DesiredEnabled);
            messages.Add($"{(op.DesiredEnabled ? "Enabled" : "Disabled")} task {task.FullName}");
        }
    }

    // ---- services ----

    private void ApplyService(TweakOperation op, TweakBackup backup, List<string> messages)
    {
        if (!_services.Exists(op.Path))
        {
            backup.Operations.Add(new TweakOperationBackup
            {
                Kind = TweakOperationKind.Service,
                Path = op.Path,
                Existed = false,
            });
            messages.Add($"Service '{op.Path}' not present.");
            return;
        }

        var original = _services.GetStartType(op.Path);
        backup.Operations.Add(new TweakOperationBackup
        {
            Kind = TweakOperationKind.Service,
            Path = op.Path,
            Existed = true,
            OriginalData = original?.ToString(),
        });

        if (op.DesiredEnabled)
        {
            _services.SetStartType(op.Path, ServiceStartupController.StartAutomatic);
            messages.Add($"Service '{op.Path}' set to Automatic.");
        }
        else
        {
            _services.SetStartType(op.Path, ServiceStartupController.StartDisabled);
            _services.Stop(op.Path);
            messages.Add($"Service '{op.Path}' disabled and stopped.");
        }
    }

    // ---- mapping / value (de)serialization ----

    private static string? SplitOriginalStart(string? originalData) =>
        originalData?.Split('|', 2)[0];

    private static Microsoft.Win32.RegistryHive MapHive(CoreHive hive) => hive switch
    {
        CoreHive.LocalMachine => Microsoft.Win32.RegistryHive.LocalMachine,
        CoreHive.CurrentUser => Microsoft.Win32.RegistryHive.CurrentUser,
        CoreHive.ClassesRoot => Microsoft.Win32.RegistryHive.ClassesRoot,
        CoreHive.Users => Microsoft.Win32.RegistryHive.Users,
        _ => Microsoft.Win32.RegistryHive.LocalMachine,
    };

    private static RegistryValueKind MapKind(CoreValueType type) => type switch
    {
        CoreValueType.String => RegistryValueKind.String,
        CoreValueType.ExpandString => RegistryValueKind.ExpandString,
        CoreValueType.Binary => RegistryValueKind.Binary,
        CoreValueType.Dword => RegistryValueKind.DWord,
        CoreValueType.MultiString => RegistryValueKind.MultiString,
        CoreValueType.Qword => RegistryValueKind.QWord,
        _ => RegistryValueKind.String,
    };

    private static CoreValueType MapKind(RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.String => CoreValueType.String,
        RegistryValueKind.ExpandString => CoreValueType.ExpandString,
        RegistryValueKind.Binary => CoreValueType.Binary,
        RegistryValueKind.DWord => CoreValueType.Dword,
        RegistryValueKind.MultiString => CoreValueType.MultiString,
        RegistryValueKind.QWord => CoreValueType.Qword,
        _ => CoreValueType.String,
    };

    private static object ParseValue(string? data, CoreValueType type)
    {
        data ??= string.Empty;
        return type switch
        {
            CoreValueType.Dword => int.TryParse(data, out var i) ? i : 0,
            CoreValueType.Qword => long.TryParse(data, out var l) ? l : 0L,
            CoreValueType.MultiString => data.Split('\n'),
            CoreValueType.Binary => Convert.FromBase64String(data),
            _ => data,
        };
    }

    private static string FormatValue(object value) => value switch
    {
        int i => i.ToString(),
        long l => l.ToString(),
        string[] arr => string.Join('\n', arr),
        byte[] bytes => Convert.ToBase64String(bytes),
        _ => value.ToString() ?? string.Empty,
    };
}
