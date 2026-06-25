using System.Management;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NvForge.Core.Abstractions;
using NvForge.Core.Gpu;
using NvForge.Core.Models;

namespace NvForge.Hardware;

/// <summary>
/// Detects installed GPUs via WMI (<c>Win32_VideoController</c>), enriched with
/// accurate VRAM read from the display-adapter registry class (WMI's
/// <c>AdapterRAM</c> is a 32-bit value and wrongly caps at ~4 GB). Only NVIDIA
/// adapters are surfaced as managed GPUs; others are ignored.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WmiGpuInfoProvider : IGpuInfoProvider
{
    // GUID of the "Display adapters" device setup class.
    private const string DisplayClassKey =
        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    public string SourceName => "WMI (Win32_VideoController)";

    public IReadOnlyList<GpuInfo> GetGpus()
    {
        var vramByDesc = ReadVramByDriverDesc();
        var result = new List<GpuInfo>();
        var index = 0;

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, DriverVersion, PNPDeviceID, AdapterRAM, AdapterCompatibility FROM Win32_VideoController");

        foreach (var mo in searcher.Get().Cast<ManagementBaseObject>())
        {
            var name = AsString(mo["Name"]);
            var vendor = AsString(mo["AdapterCompatibility"]);
            var classification = GpuClassifier.Classify(name);

            // Skip non-NVIDIA adapters (iGPUs, basic display, etc.).
            if (!classification.IsNvidia && !vendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                continue;

            var vram = vramByDesc.TryGetValue(name, out var v) && v > 0
                ? v
                : AsLong(mo["AdapterRAM"]);

            result.Add(new GpuInfo
            {
                Index = index++,
                Name = name,
                IsNvidia = true,
                FormFactor = classification.FormFactor,
                Capabilities = classification.Capabilities == CapabilityFlags.None
                    ? GpuClassifier.CapabilitiesFor(classification.FormFactor)
                    : classification.Capabilities,
                VramBytes = vram,
                DriverVersion = NormalizeDriverVersion(AsString(mo["DriverVersion"])),
                PnpDeviceId = AsString(mo["PNPDeviceID"]),
            });
        }

        return result;
    }

    /// <summary>
    /// NVIDIA's WMI DriverVersion is a long Windows version (e.g. 32.0.15.7652).
    /// The marketing version (e.g. 576.52) is the last 5 digits regrouped.
    /// We keep the raw value but append the friendly form when derivable.
    /// </summary>
    private static string NormalizeDriverVersion(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length >= 5)
        {
            var last5 = digits[^5..];
            var friendly = $"{last5[..3]}.{last5[3..]}";
            return $"{raw} ({friendly})";
        }

        return raw;
    }

    private Dictionary<string, long> ReadVramByDriverDesc()
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(DisplayClassKey);
            if (classKey is null)
                return map;

            foreach (var sub in classKey.GetSubKeyNames())
            {
                using var adapter = classKey.OpenSubKey(sub);
                if (adapter is null)
                    continue;

                var desc = adapter.GetValue("DriverDesc") as string;
                if (string.IsNullOrEmpty(desc))
                    continue;

                var vram = ReadMemorySize(adapter);
                if (vram > 0)
                    map[desc] = vram;
            }
        }
        catch
        {
            // Registry read is best-effort; fall back to WMI AdapterRAM.
        }

        return map;
    }

    private static long ReadMemorySize(RegistryKey adapter)
    {
        var raw = adapter.GetValue("HardwareInformation.qwMemorySize");
        switch (raw)
        {
            case long l:
                return l;
            case int i:
                return i;
            case byte[] bytes when bytes.Length >= 8:
                return BitConverter.ToInt64(bytes, 0);
            case byte[] bytes4 when bytes4.Length >= 4:
                return BitConverter.ToUInt32(bytes4, 0);
        }

        // Older drivers used HardwareInformation.MemorySize (DWORD bytes).
        var alt = adapter.GetValue("HardwareInformation.MemorySize");
        return alt switch
        {
            int ai => ai,
            long al => al,
            byte[] ab when ab.Length >= 4 => BitConverter.ToUInt32(ab, 0),
            _ => 0,
        };
    }

    private static string AsString(object? value) => value?.ToString()?.Trim() ?? string.Empty;

    private static long AsLong(object? value) =>
        value is null ? 0 : long.TryParse(value.ToString(), out var l) ? l : 0;
}
