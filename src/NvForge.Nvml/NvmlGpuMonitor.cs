using System.Runtime.Versioning;
using NvForge.Core.Abstractions;
using NvForge.Core.Models;

namespace NvForge.Nvml;

/// <summary>
/// Live GPU monitor backed by NVML (<c>nvml.dll</c>, installed with the driver).
/// Initializes in the constructor; if the DLL is missing or init fails (no
/// driver / not Windows), <see cref="Available"/> is false and
/// <see cref="UnavailableReason"/> explains why.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class NvmlGpuMonitor : IGpuMonitor
{
    private readonly List<MonitoredGpu> _devices = new();
    private readonly List<IntPtr> _handles = new();
    private bool _initialized;

    public NvmlGpuMonitor()
    {
        try
        {
            var init = NvmlInterop.nvmlInit_v2();
            if (init != NvmlReturn.Success)
            {
                UnavailableReason = $"NVML initialization failed ({init}). Is an NVIDIA driver installed?";
                return;
            }

            _initialized = true;

            if (NvmlInterop.nvmlDeviceGetCount_v2(out var count) != NvmlReturn.Success)
            {
                UnavailableReason = "Could not query NVIDIA device count.";
                return;
            }

            for (uint i = 0; i < count; i++)
            {
                if (NvmlInterop.nvmlDeviceGetHandleByIndex_v2(i, out var handle) != NvmlReturn.Success)
                    continue;

                var nameBuf = NvmlInterop.NameBuffer();
                var name = NvmlInterop.nvmlDeviceGetName(handle, nameBuf, NvmlInterop.NameBufferSize) == NvmlReturn.Success
                    ? nameBuf.ToString()
                    : $"NVIDIA GPU {i}";

                _handles.Add(handle);
                _devices.Add(new MonitoredGpu { Index = _devices.Count, Name = name });
            }

            if (_devices.Count == 0)
                UnavailableReason = "NVML initialized but reported no NVIDIA devices.";
        }
        catch (DllNotFoundException)
        {
            UnavailableReason = "nvml.dll not found. Install the NVIDIA driver (this is expected off real hardware).";
        }
        catch (Exception ex)
        {
            UnavailableReason = $"NVML error: {ex.Message}";
        }
    }

    public string SourceName => "NVML (nvml.dll)";

    public bool Available => _initialized && _devices.Count > 0;

    public string? UnavailableReason { get; private set; }

    public IReadOnlyList<MonitoredGpu> Devices => _devices;

    /// <summary>The driver version reported by NVML, or null.</summary>
    public string? DriverVersion
    {
        get
        {
            if (!_initialized) return null;
            var buf = NvmlInterop.DriverVersionBuffer();
            return NvmlInterop.nvmlSystemGetDriverVersion(buf, NvmlInterop.DriverVersionBufferSize) == NvmlReturn.Success
                ? buf.ToString()
                : null;
        }
    }

    public GpuSensors ReadSensors(int index)
    {
        if (index < 0 || index >= _handles.Count)
            return new GpuSensors { TimestampUtc = DateTime.UtcNow };

        var h = _handles[index];

        return new GpuSensors
        {
            TimestampUtc = DateTime.UtcNow,
            CoreClockMhz = Clock(h, NvmlClockType.Graphics),
            MemoryClockMhz = Clock(h, NvmlClockType.Mem),
            TemperatureC = Temperature(h),
            FanPercent = Fan(h),
            PowerWatts = Power(h),
            PowerLimitWatts = PowerLimit(h),
            GpuUtilPercent = Util(h, memory: false),
            MemUtilPercent = Util(h, memory: true),
            VramUsedBytes = Vram(h, used: true),
            VramTotalBytes = Vram(h, used: false),
        };
    }

    public void Dispose()
    {
        if (_initialized)
        {
            try { NvmlInterop.nvmlShutdown(); } catch { /* ignore */ }
            _initialized = false;
        }
    }

    private static int? Temperature(IntPtr h) =>
        NvmlInterop.nvmlDeviceGetTemperature(h, NvmlTemperatureSensor.Gpu, out var t) == NvmlReturn.Success ? (int)t : null;

    private static int? Fan(IntPtr h) =>
        NvmlInterop.nvmlDeviceGetFanSpeed(h, out var s) == NvmlReturn.Success ? (int)s : null;

    private static double? Power(IntPtr h) =>
        NvmlInterop.nvmlDeviceGetPowerUsage(h, out var mw) == NvmlReturn.Success ? Math.Round(mw / 1000.0, 1) : null;

    private static double? PowerLimit(IntPtr h) =>
        NvmlInterop.nvmlDeviceGetEnforcedPowerLimit(h, out var mw) == NvmlReturn.Success ? Math.Round(mw / 1000.0, 1) : null;

    private static int? Clock(IntPtr h, NvmlClockType type) =>
        NvmlInterop.nvmlDeviceGetClockInfo(h, type, out var mhz) == NvmlReturn.Success ? (int)mhz : null;

    private static int? Util(IntPtr h, bool memory)
    {
        if (NvmlInterop.nvmlDeviceGetUtilizationRates(h, out var u) != NvmlReturn.Success)
            return null;
        return (int)(memory ? u.Memory : u.Gpu);
    }

    private static long? Vram(IntPtr h, bool used)
    {
        if (NvmlInterop.nvmlDeviceGetMemoryInfo(h, out var m) != NvmlReturn.Success)
            return null;
        return (long)(used ? m.Used : m.Total);
    }
}
