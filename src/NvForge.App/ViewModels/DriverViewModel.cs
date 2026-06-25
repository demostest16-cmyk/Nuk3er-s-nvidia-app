using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvForge.Core.Drivers;
using NvForge.Core.Models;
using NvForge.Core.Tweaks;
using NvForge.DriverCustomizer;

namespace NvForge.App.ViewModels;

/// <summary>Row shown in the curated "what to strip" recommendation list.</summary>
public sealed class RecommendationRow
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Action { get; init; }
    public required string Reason { get; init; }
    public bool IsStrip { get; init; }
}

/// <summary>
/// Drives the Driver Customization tab: curated component recommendations for the
/// detected GPU, NVCleanstall detection/launch, links to the official driver
/// page, and the reversible post-install tweaks.
/// </summary>
public partial class DriverViewModel : ObservableObject
{
    private readonly NvCleanstallLocator _locator;
    private readonly DriverDownloadService _download;
    private readonly GpuInfo? _gpu;
    private readonly bool _simulated;

    private NvCleanstallLocation? _nvCleanstall;

    public DriverViewModel(
        GpuInfo? gpu,
        NvCleanstallLocator locator,
        DriverDownloadService download,
        RegistryTweakService? tweakService,
        bool simulated)
    {
        _gpu = gpu;
        _locator = locator;
        _download = download;
        _simulated = simulated;

        var formFactor = gpu?.FormFactor ?? GpuFormFactor.Desktop;
        Recommendations = new ObservableCollection<RecommendationRow>(
            DriverRecommendationBuilder.Build(formFactor).Select(r => new RecommendationRow
            {
                Name = r.Name,
                Category = r.Category,
                Action = r.Recommended == ComponentAction.Strip ? "Strip" : "Keep",
                Reason = r.Reason,
                IsStrip = r.Recommended == ComponentAction.Strip,
            }));

        Tweaks = new ObservableCollection<TweakViewModel>(
            TweakCatalog.All().Select(t => new TweakViewModel(t, tweakService, simulated)));

        RefreshNvCleanstall();
    }

    public string TargetGpuText => _gpu is null
        ? "No NVIDIA GPU detected — showing desktop defaults."
        : $"Tuned for: {_gpu.Name} ({(_gpu.FormFactor == GpuFormFactor.Mobile ? "laptop" : "desktop")})";

    public ObservableCollection<RecommendationRow> Recommendations { get; }

    public ObservableCollection<TweakViewModel> Tweaks { get; }

    [ObservableProperty]
    private string _nvCleanstallStatus = "Checking…";

    [ObservableProperty]
    private bool _nvCleanstallFound;

    [ObservableProperty]
    private string _actionStatus = string.Empty;

    [RelayCommand]
    private void RefreshNvCleanstall()
    {
        _nvCleanstall = _locator.Locate();
        NvCleanstallFound = _nvCleanstall is not null;
        NvCleanstallStatus = _nvCleanstall is null
            ? "NVCleanstall was not found. Click \"Get NVCleanstall\" to download it from TechPowerUp."
            : $"Found NVCleanstall{(string.IsNullOrEmpty(_nvCleanstall.Version) ? "" : $" v{_nvCleanstall.Version}")} at:\n{_nvCleanstall.ExecutablePath}";
    }

    [RelayCommand]
    private void LaunchNvCleanstall()
    {
        if (_simulated)
        {
            ActionStatus = "Launch skipped (simulate mode).";
            return;
        }

        if (_nvCleanstall is null)
        {
            ActionStatus = "NVCleanstall not found — download it first.";
            return;
        }

        try
        {
            _locator.Launch(_nvCleanstall.ExecutablePath);
            ActionStatus = "Launched NVCleanstall. Use the recommendations here as a guide for what to strip.";
        }
        catch (Exception ex)
        {
            ActionStatus = $"Could not launch NVCleanstall: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GetNvCleanstall()
    {
        try
        {
            _locator.OpenDownloadPage();
            ActionStatus = "Opened the NVCleanstall download page in your browser.";
        }
        catch (Exception ex)
        {
            ActionStatus = $"Could not open the page: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenDriversPage()
    {
        try
        {
            _download.OpenDownloadPage(_gpu);
            ActionStatus = "Opened NVIDIA's official driver download page.";
        }
        catch (Exception ex)
        {
            ActionStatus = $"Could not open the page: {ex.Message}";
        }
    }
}
