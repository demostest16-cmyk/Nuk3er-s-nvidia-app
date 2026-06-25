using NvForge.Core.Abstractions;
using NvForge.Core.Models;

namespace NvForge.Core.Monitoring;

/// <summary>
/// Produces plausible, gently fluctuating sensor data so the monitoring UI can
/// be exercised without a GPU (<c>--simulate</c> and CI). Values follow a simple
/// random walk within realistic bounds.
/// </summary>
public sealed class MockGpuMonitor : IGpuMonitor
{
    private sealed class State
    {
        public double Util;
        public double Temp;
        public int BaseCoreClock;
        public int MemClock;
        public double PowerLimit;
        public long VramTotal;
    }

    private readonly Random _random;
    private readonly List<MonitoredGpu> _devices;
    private readonly Dictionary<int, State> _state = new();

    public MockGpuMonitor(int? seed = null)
    {
        _random = seed is { } s ? new Random(s) : new Random();
        _devices = new List<MonitoredGpu>
        {
            new() { Index = 0, Name = "NVIDIA GeForce RTX 4060" },
            new() { Index = 1, Name = "NVIDIA GeForce RTX 5070 Ti Laptop GPU" },
        };
        _state[0] = new State { Util = 12, Temp = 42, BaseCoreClock = 2460, MemClock = 8500, PowerLimit = 115, VramTotal = 8L * 1024 * 1024 * 1024 };
        _state[1] = new State { Util = 8, Temp = 38, BaseCoreClock = 2340, MemClock = 9000, PowerLimit = 140, VramTotal = 12L * 1024 * 1024 * 1024 };
    }

    public string SourceName => "Simulated monitor (mock)";
    public bool Available => true;
    public string? UnavailableReason => null;
    public IReadOnlyList<MonitoredGpu> Devices => _devices;

    public GpuSensors ReadSensors(int index)
    {
        var s = _state.TryGetValue(index, out var st) ? st : _state[0];

        s.Util = Clamp(s.Util + (_random.NextDouble() - 0.45) * 18, 0, 100);
        var targetTemp = 38 + s.Util * 0.4;
        s.Temp = Clamp(s.Temp + (targetTemp - s.Temp) * 0.2 + (_random.NextDouble() - 0.5), 30, 90);

        var core = (int)(s.BaseCoreClock * (0.55 + 0.45 * (s.Util / 100.0)));
        var power = s.PowerLimit * (0.25 + 0.7 * (s.Util / 100.0));
        var fan = (int)Clamp((s.Temp - 35) * 2.2, 0, 100);
        var vramUsed = (long)(s.VramTotal * (0.18 + 0.5 * (s.Util / 100.0)));

        return new GpuSensors
        {
            TimestampUtc = DateTime.UtcNow,
            CoreClockMhz = core,
            MemoryClockMhz = s.MemClock,
            TemperatureC = (int)Math.Round(s.Temp),
            FanPercent = fan,
            PowerWatts = Math.Round(power, 1),
            PowerLimitWatts = s.PowerLimit,
            GpuUtilPercent = (int)Math.Round(s.Util),
            MemUtilPercent = (int)Math.Round(s.Util * 0.7),
            VramUsedBytes = vramUsed,
            VramTotalBytes = s.VramTotal,
        };
    }

    public void Dispose() { }

    private static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
}
