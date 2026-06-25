using System.Runtime.InteropServices;

namespace NvForge.NvApi;

// NVAPI exposes almost nothing as named exports. The single entry point
// `nvapi_QueryInterface(id)` returns a function pointer for a known function id,
// which we then call through a delegate. This is the standard way to use NVAPI
// from managed code (no C++ shim required). We target win-x64 only.

internal static class NvApiIds
{
    public const uint Initialize = 0x0150E828;
    public const uint Unload = 0xD22BDD7E;
    public const uint EnumPhysicalGPUs = 0xE5AC921F;
    public const uint GPU_GetFullName = 0xCEEE8E9F;
    public const uint GPU_GetPstates20 = 0x6FF81213;
    public const uint GPU_SetPstates20 = 0x0F4DAE6B;
}

internal static class NvApiConst
{
    public const int MaxPhysicalGpus = 64;
    public const int ShortStringMax = 64;
    public const int MaxPstate20Clocks = 8;
    public const int MaxPstate20BaseVoltages = 4;
    public const int MaxPstates20 = 16;

    public const uint ClockDomainGraphics = 0;
    public const uint ClockDomainMemory = 4;

    public const int NvApiOk = 0;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvInitialize();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvUnload();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvEnumPhysicalGPUs(
    [Out, MarshalAs(UnmanagedType.LPArray, SizeConst = NvApiConst.MaxPhysicalGpus)] IntPtr[] handles,
    out int count);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvGpuGetFullName(IntPtr gpu, [Out, MarshalAs(UnmanagedType.LPArray, SizeConst = NvApiConst.ShortStringMax)] byte[] name);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvGpuGetPstates20(IntPtr gpu, ref NV_GPU_PERF_PSTATES20_INFO info);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvGpuSetPstates20(IntPtr gpu, ref NV_GPU_PERF_PSTATES20_INFO info);

[StructLayout(LayoutKind.Sequential)]
internal struct NV_GPU_PERF_PSTATES20_PARAM_DELTA
{
    public int value;
    public int valueRangeMin;
    public int valueRangeMax;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NV_GPU_PSTATE20_CLOCK_ENTRY
{
    public uint domainId;
    public uint typeId;
    public uint bIsEditable;            // bitfield (1) + reserved (31) packed as one u32
    public NV_GPU_PERF_PSTATES20_PARAM_DELTA freqDelta_kHz;
    // union { single{u32 freq}; range{u32 min,max,domainId,minV,maxV}; } => 20 bytes
    public uint data0;
    public uint data1;
    public uint data2;
    public uint data3;
    public uint data4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NV_GPU_PSTATE20_BASE_VOLTAGE_ENTRY
{
    public uint domainId;
    public uint bIsEditable;
    public uint volt_uV;
    public NV_GPU_PERF_PSTATES20_PARAM_DELTA voltDelta_uV;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NV_GPU_PSTATE20
{
    public uint pstateId;
    public uint bIsEditable;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NvApiConst.MaxPstate20Clocks)]
    public NV_GPU_PSTATE20_CLOCK_ENTRY[] clocks;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NvApiConst.MaxPstate20BaseVoltages)]
    public NV_GPU_PSTATE20_BASE_VOLTAGE_ENTRY[] baseVoltages;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NV_GPU_PERF_PSTATES20_INFO
{
    public uint version;
    public uint bIsEditable;
    public uint numPstates;
    public uint numClocks;
    public uint numBaseVoltages;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NvApiConst.MaxPstates20)]
    public NV_GPU_PSTATE20[] pstates;
}

internal static class NvApiInterop
{
    [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr QueryInterface(uint id);

    public static T? GetDelegate<T>(uint id) where T : Delegate
    {
        var ptr = QueryInterface(id);
        return ptr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    /// <summary>MAKE_NVAPI_VERSION(structSize, version).</summary>
    public static uint MakeVersion<T>(uint version) where T : struct =>
        (uint)Marshal.SizeOf<T>() | (version << 16);

    /// <summary>Allocates a fully-sized (V1) PSTATES20 struct ready for marshaling.</summary>
    public static NV_GPU_PERF_PSTATES20_INFO NewPstates20Info()
    {
        var info = new NV_GPU_PERF_PSTATES20_INFO
        {
            pstates = new NV_GPU_PSTATE20[NvApiConst.MaxPstates20],
        };
        for (var i = 0; i < info.pstates.Length; i++)
        {
            info.pstates[i].clocks = new NV_GPU_PSTATE20_CLOCK_ENTRY[NvApiConst.MaxPstate20Clocks];
            info.pstates[i].baseVoltages = new NV_GPU_PSTATE20_BASE_VOLTAGE_ENTRY[NvApiConst.MaxPstate20BaseVoltages];
        }
        info.version = MakeVersion<NV_GPU_PERF_PSTATES20_INFO>(1);
        return info;
    }
}
