using System.Text.Json;

namespace NvForge.Core.Tweaks;

/// <summary>
/// Persists <see cref="TweakBackup"/> snapshots as JSON files in a directory,
/// one file per tweak id. Pure file/JSON I/O (no Windows dependency), so it is
/// exercised by unit tests on any OS. The Windows tweak service uses it to make
/// every change reversible with one click.
/// </summary>
public sealed class TweakBackupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _directory;

    public TweakBackupStore(string directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        System.IO.Directory.CreateDirectory(_directory);
    }

    public string Directory => _directory;

    public string Save(TweakBackup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);
        var path = PathFor(backup.TweakId);
        File.WriteAllText(path, JsonSerializer.Serialize(backup, JsonOptions));
        return path;
    }

    public bool Exists(string tweakId) => File.Exists(PathFor(tweakId));

    public TweakBackup? TryLoad(string tweakId)
    {
        var path = PathFor(tweakId);
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TweakBackup>(json, JsonOptions);
    }

    public bool Delete(string tweakId)
    {
        var path = PathFor(tweakId);
        if (!File.Exists(path))
            return false;
        File.Delete(path);
        return true;
    }

    public IReadOnlyList<string> ListTweakIds()
    {
        if (!System.IO.Directory.Exists(_directory))
            return Array.Empty<string>();

        return System.IO.Directory
            .EnumerateFiles(_directory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .ToList();
    }

    private string PathFor(string tweakId)
    {
        if (string.IsNullOrWhiteSpace(tweakId))
            throw new ArgumentException("Tweak id is required.", nameof(tweakId));
        return Path.Combine(_directory, Sanitize(tweakId) + ".json");
    }

    private static string Sanitize(string id)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(id.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
