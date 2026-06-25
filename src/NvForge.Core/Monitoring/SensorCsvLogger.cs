using System.Globalization;
using System.Text;
using NvForge.Core.Models;

namespace NvForge.Core.Monitoring;

/// <summary>
/// Appends GPU sensor snapshots to a CSV file. Pure file I/O (cross-platform,
/// unit-tested). Used by the monitoring view's "log to CSV" feature.
/// </summary>
public sealed class SensorCsvLogger : IDisposable
{
    public const string Header =
        "timestamp_utc,device,core_mhz,mem_mhz,temp_c,fan_pct,power_w,power_limit_w,gpu_util_pct,mem_util_pct,vram_used_mb,vram_total_mb";

    private readonly StreamWriter _writer;

    public SensorCsvLogger(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var isNew = !File.Exists(path) || new FileInfo(path).Length == 0;
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8);
        FilePath = path;
        if (isNew)
            _writer.WriteLine(Header);
    }

    public string FilePath { get; }

    public void Append(string device, GpuSensors s)
    {
        var ci = CultureInfo.InvariantCulture;
        _writer.WriteLine(string.Join(',',
            s.TimestampUtc.ToString("o", ci),
            Escape(device),
            N(s.CoreClockMhz),
            N(s.MemoryClockMhz),
            N(s.TemperatureC),
            N(s.FanPercent),
            N(s.PowerWatts),
            N(s.PowerLimitWatts),
            N(s.GpuUtilPercent),
            N(s.MemUtilPercent),
            N(s.VramUsedBytes is { } u ? u / (1024 * 1024) : (long?)null),
            N(s.VramTotalBytes is { } t ? t / (1024 * 1024) : (long?)null)));
        _writer.Flush();
    }

    public void Dispose() => _writer.Dispose();

    private static string N(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static string N(long? v) => v?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static string N(double? v) => v?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Escape(string s) => s.Contains(',') ? $"\"{s}\"" : s;
}
