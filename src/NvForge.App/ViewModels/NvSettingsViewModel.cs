using System.Collections.ObjectModel;
using NvForge.Core.Abstractions;
using NvForge.Core.DriverSettings;

namespace NvForge.App.ViewModels;

/// <summary>
/// Drives the NVIDIA Settings tab: the curated NVIDIA Control Panel settings
/// from <see cref="DriverSettingsCatalog"/>, each a reversible Apply/Restore row.
/// </summary>
public sealed class NvSettingsViewModel
{
    public NvSettingsViewModel(IDriverSettingsService service)
    {
        Available = service.Available;
        StatusMessage = service.Available
            ? $"Settings via {service.SourceName}"
            : service.UnavailableReason ?? "NVIDIA driver settings are unavailable on this system.";

        Settings = new ObservableCollection<DriverSettingViewModel>(
            service.Available
                ? DriverSettingsCatalog.All().Select(s => new DriverSettingViewModel(s, service))
                : Enumerable.Empty<DriverSettingViewModel>());
    }

    public bool Available { get; }

    public string StatusMessage { get; }

    public ObservableCollection<DriverSettingViewModel> Settings { get; }
}
