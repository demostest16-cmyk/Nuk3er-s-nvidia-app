using System.Runtime.Versioning;

namespace NvForge.DriverCustomizer;

/// <summary>State of a single scheduled task.</summary>
public readonly record struct ScheduledTaskInfo(string FullName, bool Enabled);

/// <summary>
/// Enables/disables Windows scheduled tasks via <c>schtasks.exe</c>. Used to
/// turn the NVIDIA telemetry / update-check tasks off (reversibly).
///
/// Note: task discovery parses the English-locale output of
/// <c>schtasks /query /v /fo LIST</c>. On a non-English Windows the state field
/// name differs; the operation then degrades gracefully (no tasks matched)
/// rather than failing.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ScheduledTaskController
{
    private const string TaskNameField = "TaskName:";
    private const string StateField = "Scheduled Task State:";

    /// <summary>Returns all tasks whose leaf name starts with <paramref name="leafPrefix"/>.</summary>
    public IReadOnlyList<ScheduledTaskInfo> FindByLeafPrefix(string leafPrefix)
    {
        var all = QueryAll();
        return all
            .Where(t => LeafName(t.FullName).StartsWith(leafPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public bool SetEnabled(string fullName, bool enabled)
    {
        var result = ProcessRunner.Run("schtasks", "/Change", "/TN", fullName, enabled ? "/ENABLE" : "/DISABLE");
        return result.Success;
    }

    private static IReadOnlyList<ScheduledTaskInfo> QueryAll()
    {
        var result = ProcessRunner.Run("schtasks", "/query", "/v", "/fo", "LIST");
        if (!result.Success)
            return Array.Empty<ScheduledTaskInfo>();

        var byName = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        string? currentTask = null;

        foreach (var rawLine in result.StandardOutput.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith(TaskNameField, StringComparison.OrdinalIgnoreCase))
            {
                currentTask = line[TaskNameField.Length..].Trim();
                byName.TryAdd(currentTask, true);
            }
            else if (currentTask is not null && line.StartsWith(StateField, StringComparison.OrdinalIgnoreCase))
            {
                var state = line[StateField.Length..].Trim();
                byName[currentTask] = state.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
            }
        }

        return byName.Select(kv => new ScheduledTaskInfo(kv.Key, kv.Value)).ToList();
    }

    private static string LeafName(string fullName)
    {
        var idx = fullName.LastIndexOf('\\');
        return idx >= 0 ? fullName[(idx + 1)..] : fullName;
    }
}
