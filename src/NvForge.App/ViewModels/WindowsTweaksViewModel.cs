using System.Collections.ObjectModel;
using NvForge.Core.Tweaks;
using NvForge.DriverCustomizer;

namespace NvForge.App.ViewModels;

/// <summary>
/// Drives the Windows Tweaks tab: the performance/stability/gaming tweaks from
/// <see cref="WindowsTweakCatalog"/>, plus an MSI-mode tweak targeted at the
/// detected GPU. Each row reuses <see cref="TweakViewModel"/> (apply + reversible
/// restore).
/// </summary>
public sealed class WindowsTweaksViewModel
{
    public WindowsTweaksViewModel(RegistryTweakService? tweakService, bool simulated, string? primaryGpuPnpId)
    {
        var definitions = WindowsTweakCatalog.All().ToList();
        if (!string.IsNullOrWhiteSpace(primaryGpuPnpId))
            definitions.Add(WindowsTweakCatalog.MsiModeForGpu(primaryGpuPnpId!));

        Tweaks = new ObservableCollection<TweakViewModel>(
            definitions.Select(d => new TweakViewModel(d, tweakService, simulated)));
    }

    public ObservableCollection<TweakViewModel> Tweaks { get; }
}
