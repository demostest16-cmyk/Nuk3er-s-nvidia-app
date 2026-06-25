using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using NvForge.App.ViewModels;
using NvForge.Core.Abstractions;
using NvForge.Core.Gpu;
using NvForge.Core.Models;
using NvForge.Core.Tweaks;
using NvForge.Core.Monitoring;
using NvForge.DriverCustomizer;
using NvForge.Hardware;
using NvForge.Nvml;
using Serilog;

namespace NvForge.App.Services;

/// <summary>
/// Tiny composition root: wires the providers/services from command-line args
/// and builds the root view model. Keeps App.xaml.cs minimal.
/// </summary>
[SupportedOSPlatform("windows")]
public static class AppHost
{
    public const string SimulateFlag = "--simulate";

    public static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NvForge");

    public static string LogDirectory => Path.Combine(DataDirectory, "logs");

    public static string BackupDirectory => Path.Combine(DataDirectory, "backups");

    public static bool IsSimulated(string[] args) =>
        args.Any(a => string.Equals(a, SimulateFlag, StringComparison.OrdinalIgnoreCase));

    public static MainViewModel BuildMainViewModel(string[] args)
    {
        var simulated = IsSimulated(args);
        Directory.CreateDirectory(BackupDirectory);

        IGpuInfoProvider provider = simulated ? new MockGpuInfoProvider() : new WmiGpuInfoProvider();

        IReadOnlyList<GpuInfo> gpus;
        try
        {
            gpus = provider.GetGpus();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GPU detection failed via {Source}", provider.SourceName);
            gpus = Array.Empty<GpuInfo>();
        }

        var elevated = WindowsPrivileges.IsElevated();
        var locator = new NvCleanstallLocator();
        var download = new DriverDownloadService();
        var tweakService = new RegistryTweakService(new TweakBackupStore(BackupDirectory));
        IGpuMonitor monitor = simulated ? new MockGpuMonitor() : new NvmlGpuMonitor();

        Log.Information(
            "NvForge starting. Simulated={Simulated} Elevated={Elevated} GPUs={Count} Source={Source} Monitor={Monitor}",
            simulated, elevated, gpus.Count, provider.SourceName, monitor.Available ? monitor.SourceName : "unavailable");

        return new MainViewModel(gpus, provider.SourceName, elevated, simulated, locator, download, tweakService, monitor, AppVersion);
    }

    public static string AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
}
