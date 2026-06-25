using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvForge.Core.Abstractions;
using NvForge.Core.Models;

namespace NvForge.App.ViewModels;

/// <summary>
/// One NVIDIA Control Panel setting row: Apply writes the optimized value,
/// Restore writes NVIDIA's default. Applied state is read live from the driver.
/// </summary>
public partial class DriverSettingViewModel : ObservableObject
{
    private readonly DriverSetting _setting;
    private readonly IDriverSettingsService _service;

    public DriverSettingViewModel(DriverSetting setting, IDriverSettingsService service)
    {
        _setting = setting;
        _service = service;
        _isApplied = service.Read(setting.SettingId) == setting.OptimizedValue;
        _status = _isApplied ? "Optimized" : "Default";
    }

    public string Name => _setting.NvcpName;
    public string Description => _setting.Description;

    [ObservableProperty] private bool _isApplied;
    [ObservableProperty] private string _status;

    [RelayCommand]
    private async Task ApplyAsync()
    {
        var result = await Task.Run(() => _service.Write(_setting.SettingId, _setting.OptimizedValue));
        if (result.Success)
            IsApplied = true;
        Status = result.Success ? "Optimized" : result.Message;
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        var result = await Task.Run(() => _service.Write(_setting.SettingId, _setting.DefaultValue));
        if (result.Success)
            IsApplied = false;
        Status = result.Success ? "Restored to default" : result.Message;
    }
}
