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

    // Driver Settings (DRS)
    public const uint DRS_CreateSession = 0x0694D52E;
    public const uint DRS_DestroySession = 0xDAD9CFF8;
    public const uint DRS_LoadSettings = 0x375DBD6B;
    public const uint DRS_SaveSettings = 0xFCBC7E14;
    public const uint DRS_GetBaseProfile = 0xDA8466A0;
    public const uint DRS_GetSetting = 0x73BF8338;
    public const uint DRS_SetSetting = 0x577DD202;
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
    public const int NvApiSettingNotFound = -163; // NVAPI_SETTING_NOT_FOUND

    // DRS NVDRS_SETTING_V1 field sizes.
    public const int UnicodeStringMax = 2048;      // NvU16[2048] = 4096 bytes
    public const int SettingValueUnionBytes = 4100; // max(u32, unicode 4096, binary{len+4096})
    public const uint DwordSettingType = 0;         // NVDRS_DWORD_TYPE
    public const uint CurrentProfileLocation = 0;   // NVDRS_CURRENT_PROFILE_LOCATION
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

// NVDRS_SETTING_V1. The two value unions (predefined / current) are modeled as
// raw 4100-byte buffers; for DWORD settings the value is the first 4 bytes.
[StructLayout(LayoutKind.Sequential)]
internal struct NVDRS_SETTING
{
    public uint version;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NvApiConst.UnicodeStringMax)]
    public ushort[] settingName;

    public uint settingId;
    public uint settingType;       // NVDRS_SETTING_TYPE
    public uint settingLocation;   // NVDRS_SETTING_LOCATION
    public uint isCurrentPredefined;
    public uint isPredefinedValid;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NvApiConst.SettingValueUnionBytes)]
    public byte[] predefinedValue;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NvApiConst.SettingValueUnionBytes)]
    public byte[] currentValue;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvDrsCreateSession(out IntPtr session);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvDrsDestroySession(IntPtr session);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvDrsLoadSettings(IntPtr session);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvDrsSaveSettings(IntPtr session);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvDrsGetBaseProfile(IntPtr session, out IntPtr profile);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvDrsGetSetting(IntPtr session, IntPtr profile, uint settingId, ref NVDRS_SETTING setting);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int NvDrsSetSetting(IntPtr session, IntPtr profile, ref NVDRS_SETTING setting);

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

    /// <summary>Allocates a fully-sized (V1) NVDRS_SETTING ready for marshaling.</summary>
    public static NVDRS_SETTING NewSetting() => new()
    {
        version = MakeVersion<NVDRS_SETTING>(1),
        settingName = new ushort[NvApiConst.UnicodeStringMax],
        predefinedValue = new byte[NvApiConst.SettingValueUnionBytes],
        currentValue = new byte[NvApiConst.SettingValueUnionBytes],
    };
}
