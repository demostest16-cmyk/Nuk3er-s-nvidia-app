using System.Runtime.InteropServices;
using System.Text;

namespace NvForge.Nvml;

/// <summary>NVML return codes (subset). 0 == success.</summary>
internal enum NvmlReturn
{
    Success = 0,
    Uninitialized = 1,
    InvalidArgument = 2,
    NotSupported = 3,
    NoPermission = 4,
    NotFound = 6,
    DriverNotLoaded = 9,
}

internal enum NvmlTemperatureSensor
{
    Gpu = 0,
}

internal enum NvmlClockType
{
    Graphics = 0,
    Sm = 1,
    Mem = 2,
    Video = 3,
}

[StructLayout(LayoutKind.Sequential)]
internal struct NvmlUtilization
{
    public uint Gpu;
    public uint Memory;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NvmlMemory
{
    public ulong Total;
    public ulong Free;
    public ulong Used;
}

/// <summary>
/// Raw P/Invoke surface for nvml.dll (ships with the NVIDIA driver). Only the
/// functions needed for live monitoring are declared. We target win-x64 only, so
/// the calling convention is unified and need not be specified. These compile on
/// any OS; the DLL is resolved at runtime on Windows.
/// </summary>
internal static class NvmlInterop
{
    private const string Dll = "nvml.dll";
    private const int NvmlDeviceNameBufferSize = 96;
    private const int NvmlSystemDriverVersionBufferSize = 80;

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlInit_v2();

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlShutdown();

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetCount_v2(out uint deviceCount);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

    [DllImport(Dll, CharSet = CharSet.Ansi)]
    public static extern NvmlReturn nvmlDeviceGetName(IntPtr device, StringBuilder name, uint length);

    [DllImport(Dll, CharSet = CharSet.Ansi)]
    public static extern NvmlReturn nvmlSystemGetDriverVersion(StringBuilder version, uint length);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetTemperature(IntPtr device, NvmlTemperatureSensor sensorType, out uint temp);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetFanSpeed(IntPtr device, out uint speed);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetPowerUsage(IntPtr device, out uint milliwatts);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetEnforcedPowerLimit(IntPtr device, out uint limitMilliwatts);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetClockInfo(IntPtr device, NvmlClockType type, out uint clockMhz);

    [DllImport(Dll)]
    public static extern NvmlReturn nvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

    public static StringBuilder NameBuffer() => new(NvmlDeviceNameBufferSize);
    public static StringBuilder DriverVersionBuffer() => new(NvmlSystemDriverVersionBufferSize);
    public static uint NameBufferSize => NvmlDeviceNameBufferSize;
    public static uint DriverVersionBufferSize => NvmlSystemDriverVersionBufferSize;
}
