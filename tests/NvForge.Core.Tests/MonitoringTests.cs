using NvForge.Core.Models;
using NvForge.Core.Monitoring;
using Xunit;

namespace NvForge.Core.Tests;

public class MockGpuMonitorTests
{
    [Fact]
    public void ExposesTwoDevices_AndIsAvailable()
    {
        using var monitor = new MockGpuMonitor(seed: 1);
        Assert.True(monitor.Available);
        Assert.Equal(2, monitor.Devices.Count);
        Assert.Contains(monitor.Devices, d => d.Name.Contains("4060"));
        Assert.Contains(monitor.Devices, d => d.Name.Contains("5070"));
    }

    [Fact]
    public void ReadSensors_ProducesValuesInRealisticRanges()
    {
        using var monitor = new MockGpuMonitor(seed: 42);
        for (var i = 0; i < 50; i++)
        {
            var s = monitor.ReadSensors(0);
            Assert.InRange(s.GpuUtilPercent!.Value, 0, 100);
            Assert.InRange(s.TemperatureC!.Value, 20, 100);
            Assert.InRange(s.FanPercent!.Value, 0, 100);
            Assert.True(s.PowerWatts > 0);
            Assert.True(s.VramUsedBytes <= s.VramTotalBytes);
        }
    }
}

public class SensorCsvLoggerTests : IDisposable
{
    private readonly string _path;

    public SensorCsvLoggerTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "nvforge-tests", Guid.NewGuid().ToString("N"), "sensors.csv");
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_path);
        if (dir is not null && Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void WritesHeaderOnce_AndAppendsRows()
    {
        var sensors = new GpuSensors
        {
            TimestampUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CoreClockMhz = 2460,
            MemoryClockMhz = 8500,
            TemperatureC = 55,
            FanPercent = 40,
            PowerWatts = 92.5,
            PowerLimitWatts = 115,
            GpuUtilPercent = 73,
            MemUtilPercent = 50,
            VramUsedBytes = 4L * 1024 * 1024 * 1024,
            VramTotalBytes = 8L * 1024 * 1024 * 1024,
        };

        using (var logger = new SensorCsvLogger(_path))
        {
            logger.Append("RTX 4060", sensors);
            logger.Append("RTX 4060", sensors);
        }

        var lines = File.ReadAllLines(_path);
        Assert.Equal(3, lines.Length); // header + 2 rows
        Assert.Equal(SensorCsvLogger.Header, lines[0]);
        Assert.Contains("RTX 4060", lines[1]);
        Assert.Contains("92.5", lines[1]);   // invariant-culture decimal
        Assert.Contains("4096", lines[1]);   // VRAM used in MB
    }
}
