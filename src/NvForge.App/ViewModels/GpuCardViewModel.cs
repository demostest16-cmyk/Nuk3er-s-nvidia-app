using NvForge.Core.Models;

namespace NvForge.App.ViewModels;

/// <summary>Read-only presentation of one detected GPU on the dashboard.</summary>
public sealed class GpuCardViewModel
{
    public GpuCardViewModel(GpuInfo gpu)
    {
        Gpu = gpu;
    }

    public GpuInfo Gpu { get; }

    public string Name => Gpu.Name;

    public string FormFactorText => Gpu.FormFactor switch
    {
        GpuFormFactor.Desktop => "Desktop",
        GpuFormFactor.Mobile => "Laptop / Mobile",
        _ => "Unknown",
    };

    public string VramText => Gpu.VramBytes > 0 ? $"{Gpu.VramGigabytes:0.#} GB" : "Unknown";

    public string DriverVersionText =>
        string.IsNullOrEmpty(Gpu.DriverVersion) ? "Unknown" : Gpu.DriverVersion;

    public string CapabilitiesText
    {
        get
        {
            var caps = new List<string>();
            if (Gpu.Supports(CapabilityFlags.Monitoring)) caps.Add("Monitoring");
            if (Gpu.Supports(CapabilityFlags.CoreClockOffset)) caps.Add("Core OC");
            if (Gpu.Supports(CapabilityFlags.MemoryClockOffset)) caps.Add("Memory OC");
            if (Gpu.Supports(CapabilityFlags.PowerLimit)) caps.Add("Power limit");
            if (Gpu.Supports(CapabilityFlags.TempLimit)) caps.Add("Temp limit");
            if (Gpu.Supports(CapabilityFlags.FanControl)) caps.Add("Fan control");
            if (Gpu.Supports(CapabilityFlags.VoltageControl)) caps.Add("Voltage");
            if (Gpu.Supports(CapabilityFlags.DriverCustomization)) caps.Add("Driver tweaks");
            return caps.Count == 0 ? "None detected" : string.Join("  •  ", caps);
        }
    }

    public bool IsVendorLocked => Gpu.IsVendorLocked;

    public string LockNote => IsVendorLocked
        ? "Mobile GPU: power, thermal and voltage controls are usually locked by the laptop vendor."
        : string.Empty;
}
