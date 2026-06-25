using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvForge.App.Services;
using NvForge.Core.Abstractions;
using NvForge.Core.Models;
using NvForge.Core.Monitoring;

namespace NvForge.App.ViewModels;

/// <summary>
/// Polls the live GPU monitor once a second and exposes the readings, rolling
/// min/avg/max, a utilization sparkline, and optional CSV logging. Falls back to
/// a clear status message when no monitor is available.
/// </summary>
public partial class MonitoringViewModel : ObservableObject
{
    private const int HistoryLength = 120;

    private readonly IGpuMonitor _monitor;
    private readonly DispatcherTimer? _timer;
    private readonly List<double> _utilHistory = new();

    private readonly Stat _tempStat = new();
    private readonly Stat _utilStat = new();
    private readonly Stat _powerStat = new();

    private SensorCsvLogger? _logger;

    public MonitoringViewModel(IGpuMonitor monitor)
    {
        _monitor = monitor;
        Available = monitor.Available;

        if (!Available)
        {
            StatusMessage = monitor.UnavailableReason ?? "No GPU monitor available on this system.";
            return;
        }

        Devices = monitor.Devices.Select(d => d.Name).ToList();
        StatusMessage = $"Live via {monitor.SourceName}";

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        Tick();
    }

    public bool Available { get; }

    public IReadOnlyList<string> Devices { get; } = Array.Empty<string>();

    [ObservableProperty]
    private int _selectedDeviceIndex;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _coreClockText = "—";
    [ObservableProperty] private string _memClockText = "—";
    [ObservableProperty] private string _tempText = "—";
    [ObservableProperty] private string _fanText = "—";
    [ObservableProperty] private string _powerText = "—";
    [ObservableProperty] private string _vramText = "—";

    [ObservableProperty] private double _utilPercent;
    [ObservableProperty] private double _memUtilPercent;
    [ObservableProperty] private double _powerPercent;

    [ObservableProperty] private string _tempStatsText = string.Empty;
    [ObservableProperty] private string _utilStatsText = string.Empty;
    [ObservableProperty] private string _powerStatsText = string.Empty;

    [ObservableProperty] private PointCollection _utilizationPoints = new();

    [ObservableProperty] private bool _isLogging;
    [ObservableProperty] private string _loggingStatus = string.Empty;

    partial void OnSelectedDeviceIndexChanged(int value) => ResetStats();

    private void Tick()
    {
        if (SelectedDeviceIndex < 0 || SelectedDeviceIndex >= _monitor.Devices.Count)
            return;

        GpuSensors s;
        try
        {
            s = _monitor.ReadSensors(SelectedDeviceIndex);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sensor read failed: {ex.Message}";
            return;
        }

        CoreClockText = s.CoreClockMhz is { } c ? $"{c} MHz" : "—";
        MemClockText = s.MemoryClockMhz is { } m ? $"{m} MHz" : "—";
        TempText = s.TemperatureC is { } t ? $"{t} °C" : "—";
        FanText = s.FanPercent is { } f ? $"{f} %" : "—";
        PowerText = s.PowerWatts is { } p
            ? (s.PowerLimitWatts is { } l ? $"{p:0.#} / {l:0.#} W" : $"{p:0.#} W")
            : "—";
        VramText = s.VramUsedGb is { } used && s.VramTotalGb is { } total
            ? $"{used:0.#} / {total:0.#} GB"
            : "—";

        UtilPercent = s.GpuUtilPercent ?? 0;
        MemUtilPercent = s.MemUtilPercent ?? 0;
        PowerPercent = s.PowerPercent ?? 0;

        if (s.TemperatureC is { } tc) _tempStat.Add(tc);
        if (s.GpuUtilPercent is { } gu) _utilStat.Add(gu);
        if (s.PowerWatts is { } pw) _powerStat.Add(pw);

        TempStatsText = _tempStat.Format("°C");
        UtilStatsText = _utilStat.Format("%");
        PowerStatsText = _powerStat.Format("W");

        PushHistory(s.GpuUtilPercent ?? 0);

        if (IsLogging && _logger is not null)
        {
            try { _logger.Append(Devices.ElementAtOrDefault(SelectedDeviceIndex) ?? "GPU", s); }
            catch (Exception ex) { LoggingStatus = $"Logging error: {ex.Message}"; }
        }
    }

    private void PushHistory(double util)
    {
        _utilHistory.Add(util);
        if (_utilHistory.Count > HistoryLength)
            _utilHistory.RemoveAt(0);

        var pts = new PointCollection(_utilHistory.Count);
        for (var i = 0; i < _utilHistory.Count; i++)
            pts.Add(new Point(i, 100 - Math.Clamp(_utilHistory[i], 0, 100)));
        UtilizationPoints = pts;
    }

    [RelayCommand]
    private void ResetStats()
    {
        _tempStat.Reset();
        _utilStat.Reset();
        _powerStat.Reset();
        _utilHistory.Clear();
        UtilizationPoints = new PointCollection();
        TempStatsText = UtilStatsText = PowerStatsText = string.Empty;
    }

    [RelayCommand]
    private void ToggleLogging()
    {
        if (IsLogging)
        {
            _logger?.Dispose();
            _logger = null;
            IsLogging = false;
            LoggingStatus = "Logging stopped.";
            return;
        }

        try
        {
            Directory.CreateDirectory(AppHost.LogDirectory);
            var file = Path.Combine(AppHost.LogDirectory, $"sensors-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            _logger = new SensorCsvLogger(file);
            IsLogging = true;
            LoggingStatus = $"Logging to {file}";
        }
        catch (Exception ex)
        {
            LoggingStatus = $"Could not start logging: {ex.Message}";
        }
    }

    public void Stop()
    {
        _timer?.Stop();
        _logger?.Dispose();
        _monitor.Dispose();
    }

    /// <summary>Running min/avg/max accumulator.</summary>
    private sealed class Stat
    {
        private double _min, _max, _sum;
        private int _count;

        public void Add(double v)
        {
            if (_count == 0) { _min = _max = v; }
            else { if (v < _min) _min = v; if (v > _max) _max = v; }
            _sum += v;
            _count++;
        }

        public void Reset() => (_min, _max, _sum, _count) = (0, 0, 0, 0);

        public string Format(string unit) =>
            _count == 0 ? "" : $"min {_min:0.#}{unit}   avg {_sum / _count:0.#}{unit}   max {_max:0.#}{unit}";
    }
}
