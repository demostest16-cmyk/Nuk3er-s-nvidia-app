using System.Runtime.Versioning;
using System.Text;
using NvForge.Core.Abstractions;
using NvForge.Core.Models;

namespace NvForge.NvApi;

/// <summary>
/// Overclock tuner backed by NVAPI (<c>nvapi64.dll</c>, installed with the
/// driver). Reads and applies core/memory clock offsets via Pstates20.
///
/// IMPORTANT: this write path is implemented from the public NVAPI struct
/// definitions but cannot be validated without real hardware. NVAPI validates
/// the struct version, so a mismatch fails cleanly (returns an error, applies
/// nothing) rather than corrupting state. The UI wraps Apply with a
/// test-then-auto-revert safety.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class NvApiGpuTuner : IGpuTuner
{
    private readonly List<MonitoredGpu> _devices = new();
    private readonly List<IntPtr> _handles = new();
    private readonly NvGpuGetPstates20? _getPstates;
    private readonly NvGpuSetPstates20? _setPstates;
    private readonly NvUnload? _unload;
    private bool _initialized;

    public NvApiGpuTuner()
    {
        try
        {
            var initialize = NvApiInterop.GetDelegate<NvInitialize>(NvApiIds.Initialize);
            if (initialize is null)
            {
                UnavailableReason = "NVAPI entry point not found.";
                return;
            }

            if (initialize() != NvApiConst.NvApiOk)
            {
                UnavailableReason = "NVAPI_Initialize failed. Is an NVIDIA driver installed?";
                return;
            }

            _initialized = true;
            _unload = NvApiInterop.GetDelegate<NvUnload>(NvApiIds.Unload);
            _getPstates = NvApiInterop.GetDelegate<NvGpuGetPstates20>(NvApiIds.GPU_GetPstates20);
            _setPstates = NvApiInterop.GetDelegate<NvGpuSetPstates20>(NvApiIds.GPU_SetPstates20);

            var enumGpus = NvApiInterop.GetDelegate<NvEnumPhysicalGPUs>(NvApiIds.EnumPhysicalGPUs);
            var getName = NvApiInterop.GetDelegate<NvGpuGetFullName>(NvApiIds.GPU_GetFullName);
            if (enumGpus is null)
            {
                UnavailableReason = "NVAPI_EnumPhysicalGPUs unavailable.";
                return;
            }

            var handles = new IntPtr[NvApiConst.MaxPhysicalGpus];
            if (enumGpus(handles, out var count) != NvApiConst.NvApiOk)
            {
                UnavailableReason = "Could not enumerate NVIDIA GPUs.";
                return;
            }

            for (var i = 0; i < count; i++)
            {
                _handles.Add(handles[i]);
                var name = $"NVIDIA GPU {i}";
                if (getName is not null)
                {
                    var buffer = new byte[NvApiConst.ShortStringMax];
                    if (getName(handles[i], buffer) == NvApiConst.NvApiOk)
                        name = Encoding.ASCII.GetString(buffer).TrimEnd('\0', ' ');
                }
                _devices.Add(new MonitoredGpu { Index = _devices.Count, Name = name });
            }

            if (_devices.Count == 0)
                UnavailableReason = "NVAPI reported no NVIDIA GPUs.";
        }
        catch (DllNotFoundException)
        {
            UnavailableReason = "nvapi64.dll not found. Install the NVIDIA driver (expected off real hardware).";
        }
        catch (Exception ex)
        {
            UnavailableReason = $"NVAPI error: {ex.Message}";
        }
    }

    public string SourceName => "NVAPI (nvapi64.dll)";
    public bool Available => _initialized && _devices.Count > 0 && _getPstates is not null && _setPstates is not null;
    public string? UnavailableReason { get; private set; }
    public IReadOnlyList<MonitoredGpu> Devices => _devices;

    public TuningState ReadState(int index)
    {
        if (!Available || index < 0 || index >= _handles.Count || _getPstates is null)
            return new TuningState();

        var info = NvApiInterop.NewPstates20Info();
        if (_getPstates(_handles[index], ref info) != NvApiConst.NvApiOk)
            return new TuningState();

        var p0 = FindP0(info);
        var core = ReadClock(info, p0, NvApiConst.ClockDomainGraphics);
        var mem = ReadClock(info, p0, NvApiConst.ClockDomainMemory);

        return new TuningState
        {
            CoreEditable = core.editable,
            CoreOffsetMhz = core.offsetMhz,
            CoreMinMhz = core.minMhz,
            CoreMaxMhz = core.maxMhz,
            MemoryEditable = mem.editable,
            MemoryOffsetMhz = mem.offsetMhz,
            MemoryMinMhz = mem.minMhz,
            MemoryMaxMhz = mem.maxMhz,
        };
    }

    public TuningApplyResult ApplyOffsets(int index, int coreOffsetMhz, int memoryOffsetMhz)
    {
        if (!Available || _setPstates is null)
            return new TuningApplyResult { Success = false, Message = UnavailableReason ?? "NVAPI tuning unavailable." };
        if (index < 0 || index >= _handles.Count)
            return new TuningApplyResult { Success = false, Message = "Invalid GPU index." };

        var info = NvApiInterop.NewPstates20Info();
        info.numPstates = 1;
        info.numClocks = 2;
        info.numBaseVoltages = 0;

        info.pstates[0].pstateId = 0; // P0
        info.pstates[0].bIsEditable = 1;

        info.pstates[0].clocks[0].domainId = NvApiConst.ClockDomainGraphics;
        info.pstates[0].clocks[0].typeId = 0;
        info.pstates[0].clocks[0].bIsEditable = 1;
        info.pstates[0].clocks[0].freqDelta_kHz.value = coreOffsetMhz * 1000;

        info.pstates[0].clocks[1].domainId = NvApiConst.ClockDomainMemory;
        info.pstates[0].clocks[1].typeId = 0;
        info.pstates[0].clocks[1].bIsEditable = 1;
        info.pstates[0].clocks[1].freqDelta_kHz.value = memoryOffsetMhz * 1000;

        var status = _setPstates(_handles[index], ref info);
        return status == NvApiConst.NvApiOk
            ? new TuningApplyResult { Success = true, Message = $"Applied core {coreOffsetMhz:+#;-#;0} MHz, memory {memoryOffsetMhz:+#;-#;0} MHz." }
            : new TuningApplyResult { Success = false, Message = $"NVAPI_SetPstates20 returned status {status}. (Often means locked or unsupported on this GPU.)" };
    }

    public void Dispose()
    {
        if (_initialized)
        {
            try { _unload?.Invoke(); } catch { /* ignore */ }
            _initialized = false;
        }
    }

    private static int FindP0(NV_GPU_PERF_PSTATES20_INFO info)
    {
        var n = (int)Math.Min(info.numPstates, (uint)info.pstates.Length);
        for (var i = 0; i < n; i++)
            if (info.pstates[i].pstateId == 0)
                return i;
        return 0;
    }

    private static (bool editable, int offsetMhz, int minMhz, int maxMhz) ReadClock(
        NV_GPU_PERF_PSTATES20_INFO info, int pstateIndex, uint domainId)
    {
        var pstate = info.pstates[pstateIndex];
        var n = (int)Math.Min(info.numClocks, (uint)pstate.clocks.Length);
        for (var i = 0; i < n; i++)
        {
            var c = pstate.clocks[i];
            if (c.domainId != domainId)
                continue;
            return (
                c.bIsEditable != 0,
                c.freqDelta_kHz.value / 1000,
                c.freqDelta_kHz.valueRangeMin / 1000,
                c.freqDelta_kHz.valueRangeMax / 1000);
        }
        return (false, 0, 0, 0);
    }
}
