using System.Text.Json;

namespace NvForge.Core.Tuning;

/// <summary>
/// Persists overclock profiles to a single JSON file (upsert by name). Pure
/// file/JSON I/O, unit-tested on any OS.
/// </summary>
public sealed class OverclockProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;

    public OverclockProfileStore(string filePath)
    {
        _path = filePath ?? throw new ArgumentNullException(nameof(filePath));
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public IReadOnlyList<OverclockProfile> GetAll()
    {
        if (!File.Exists(_path))
            return Array.Empty<OverclockProfile>();
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<List<OverclockProfile>>(json, JsonOptions) ?? new List<OverclockProfile>();
    }

    public OverclockProfile? TryGet(string name) =>
        GetAll().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds or replaces a profile by name (case-insensitive).</summary>
    public void Save(OverclockProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.Name))
            throw new ArgumentException("Profile name is required.", nameof(profile));

        var list = GetAll().Where(p => !string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        list.Add(profile);
        Write(list);
    }

    public bool Delete(string name)
    {
        var list = GetAll().ToList();
        var removed = list.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed)
            Write(list);
        return removed;
    }

    private void Write(List<OverclockProfile> list) =>
        File.WriteAllText(_path, JsonSerializer.Serialize(list, JsonOptions));
}
