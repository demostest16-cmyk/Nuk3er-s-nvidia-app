using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvForge.Core.Tweaks;
using NvForge.DriverCustomizer;

namespace NvForge.App.ViewModels;

/// <summary>
/// One reversible post-install tweak (e.g. disable telemetry). Apply/Restore run
/// off the UI thread; in simulate mode they are no-ops so the UI can be exercised
/// on a machine without touching its real registry/services.
/// </summary>
public partial class TweakViewModel : ObservableObject
{
    private readonly TweakDefinition _definition;
    private readonly RegistryTweakService? _service;
    private readonly bool _simulated;

    public TweakViewModel(TweakDefinition definition, RegistryTweakService? service, bool simulated)
    {
        _definition = definition;
        _service = service;
        _simulated = simulated;
        _isApplied = service?.IsApplied(definition.Id) ?? false;
        _status = _isApplied ? "Applied" : "Not applied";
    }

    public string Name => _definition.Name;
    public string Category => _definition.Category;
    public string Description => _definition.Description;

    [ObservableProperty]
    private bool _isApplied;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (_simulated || _service is null)
        {
            IsApplied = true;
            Status = "Applied (simulated)";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await Task.Run(() => _service.Apply(_definition));
            IsApplied = true;
            Status = result.Messages.Count > 0 ? string.Join("; ", result.Messages) : "Applied";
        }
        catch (Exception ex)
        {
            Status = $"Failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (_simulated || _service is null)
        {
            IsApplied = false;
            Status = "Restored (simulated)";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await Task.Run(() => _service.Restore(_definition.Id));
            IsApplied = false;
            Status = result.Success ? "Restored to original state" : string.Join("; ", result.Messages);
        }
        catch (Exception ex)
        {
            Status = $"Failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
