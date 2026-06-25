using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NvForge.Core.Abstractions;
using NvForge.Core.Models;
using NvForge.Core.Tuning;
using NvForge.DriverCustomizer;

namespace NvForge.App.ViewModels;

/// <summary>Root view model for the main window.</summary>
public partial class MainViewModel : ObservableObject
{
    public MainViewModel(
        IReadOnlyList<GpuInfo> gpus,
        string source,
        bool elevated,
        bool simulated,
        NvCleanstallLocator locator,
        DriverDownloadService download,
        RegistryTweakService? tweakService,
        IGpuMonitor monitor,
        IGpuTuner tuner,
        OverclockProfileStore profiles,
        string appVersion)
    {
        AppVersion = appVersion;
        Gpus = new ObservableCollection<GpuCardViewModel>(gpus.Select(g => new GpuCardViewModel(g)));

        var primaryGpu = gpus.Count > 0 ? gpus[0] : null;
        Driver = new DriverViewModel(primaryGpu, locator, download, tweakService, simulated);
        Monitoring = new MonitoringViewModel(monitor);
        Tuning = new TuningViewModel(tuner, profiles);
        Diagnostics = new DiagnosticsViewModel(gpus, source, elevated, simulated, appVersion);

        var banners = new List<string>();
        if (simulated)
            banners.Add("SIMULATE MODE — showing mock GPUs; tweaks and launches are disabled.");
        if (!elevated && !simulated)
            banners.Add("Not running as Administrator — driver tweaks will fail. Restart NvForge as admin.");
        if (gpus.Count == 0 && !simulated)
            banners.Add("No NVIDIA GPU detected. Driver tweaks still work, but verify your driver is installed.");

        BannerText = string.Join("  ", banners);
        ShowBanner = banners.Count > 0;
    }

    public string AppVersion { get; }

    public string Title => $"NvForge {AppVersion} — NVIDIA GPU Manager";

    public ObservableCollection<GpuCardViewModel> Gpus { get; }

    public bool HasGpus => Gpus.Count > 0;

    public bool NoGpus => !HasGpus;

    public DriverViewModel Driver { get; }

    public MonitoringViewModel Monitoring { get; }

    public TuningViewModel Tuning { get; }

    public DiagnosticsViewModel Diagnostics { get; }

    /// <summary>Stops timers and releases NVML/NVAPI sessions on window close.</summary>
    public void Shutdown()
    {
        Monitoring.Stop();
        Tuning.Dispose();
    }

    public bool ShowBanner { get; }

    public string BannerText { get; }
}
