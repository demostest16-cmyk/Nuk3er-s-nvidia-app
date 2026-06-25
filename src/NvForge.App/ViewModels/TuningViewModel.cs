using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvForge.Core.Abstractions;
using NvForge.Core.Tuning;

namespace NvForge.App.ViewModels;

/// <summary>
/// Drives the Tuning tab: core/memory clock-offset sliders (capability-gated),
/// Apply with a 15-second test-then-auto-revert safety, and saved profiles.
/// </summary>
public partial class TuningViewModel : ObservableObject
{
    public const string ExperimentalNote =
        "Overclocking is experimental and applied at your own risk. Apply starts a 15-second test — " +
        "click Keep to confirm, otherwise the offset auto-reverts. Laptop GPUs are usually locked by the vendor.";

    private const int RevertSeconds = 15;

    private readonly IGpuTuner _tuner;
    private readonly OverclockProfileStore _profiles;
    private readonly DispatcherTimer? _revertTimer;

    private int _confirmedCore;
    private int _confirmedMem;

    public TuningViewModel(IGpuTuner tuner, OverclockProfileStore profiles)
    {
        _tuner = tuner;
        _profiles = profiles;
        Available = tuner.Available;

        ProfileNames = new ObservableCollection<string>(profiles.GetAll().Select(p => p.Name));

        if (!Available)
        {
            StatusMessage = tuner.UnavailableReason ?? "No tuning backend available on this system.";
            return;
        }

        Devices = tuner.Devices.Select(d => d.Name).ToList();
        StatusMessage = $"Tuning via {tuner.SourceName}";

        _revertTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _revertTimer.Tick += (_, _) => RevertTick();

        LoadState();
    }

    public bool Available { get; }
    public IReadOnlyList<string> Devices { get; } = Array.Empty<string>();
    public ObservableCollection<string> ProfileNames { get; }

    [ObservableProperty] private int _selectedDeviceIndex;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private int _coreOffset;
    [ObservableProperty] private int _memoryOffset;
    [ObservableProperty] private int _coreMin;
    [ObservableProperty] private int _coreMax;
    [ObservableProperty] private int _memoryMin;
    [ObservableProperty] private int _memoryMax;
    [ObservableProperty] private bool _coreEditable;
    [ObservableProperty] private bool _memoryEditable;
    [ObservableProperty] private bool _canApply;
    [ObservableProperty] private string _lockNote = string.Empty;

    [ObservableProperty] private string _applyStatus = string.Empty;
    [ObservableProperty] private bool _revertPending;
    [ObservableProperty] private int _revertSecondsLeft;

    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private string? _selectedProfile;

    partial void OnSelectedDeviceIndexChanged(int value) => LoadState();

    private void LoadState()
    {
        if (!Available || SelectedDeviceIndex < 0 || SelectedDeviceIndex >= _tuner.Devices.Count)
            return;

        var state = _tuner.ReadState(SelectedDeviceIndex);
        CoreEditable = state.CoreEditable;
        MemoryEditable = state.MemoryEditable;
        CoreMin = state.CoreMinMhz;
        CoreMax = state.CoreMaxMhz == 0 && state.CoreMinMhz == 0 ? 0 : state.CoreMaxMhz;
        MemoryMin = state.MemoryMinMhz;
        MemoryMax = state.MemoryMaxMhz;
        CoreOffset = state.CoreOffsetMhz;
        MemoryOffset = state.MemoryOffsetMhz;
        _confirmedCore = state.CoreOffsetMhz;
        _confirmedMem = state.MemoryOffsetMhz;

        CanApply = state.AnyEditable;
        LockNote = state.AnyEditable
            ? string.Empty
            : "This GPU's clocks are locked (typical for laptop/mobile parts) — offsets cannot be applied.";
    }

    [RelayCommand]
    private void Apply()
    {
        var result = _tuner.ApplyOffsets(SelectedDeviceIndex, CoreOffset, MemoryOffset);
        ApplyStatus = result.Message;
        if (!result.Success)
            return;

        RevertPending = true;
        RevertSecondsLeft = RevertSeconds;
        _revertTimer?.Start();
    }

    [RelayCommand]
    private void Keep()
    {
        _revertTimer?.Stop();
        RevertPending = false;
        _confirmedCore = CoreOffset;
        _confirmedMem = MemoryOffset;
        ApplyStatus = $"Kept core {CoreOffset:+#;-#;0} MHz, memory {MemoryOffset:+#;-#;0} MHz.";
    }

    [RelayCommand]
    private void Reset()
    {
        _revertTimer?.Stop();
        RevertPending = false;
        CoreOffset = 0;
        MemoryOffset = 0;
        var result = _tuner.ApplyOffsets(SelectedDeviceIndex, 0, 0);
        _confirmedCore = 0;
        _confirmedMem = 0;
        ApplyStatus = result.Success ? "Reset to stock (0 / 0)." : result.Message;
    }

    private void RevertTick()
    {
        RevertSecondsLeft--;
        if (RevertSecondsLeft > 0)
            return;

        _revertTimer?.Stop();
        RevertPending = false;
        CoreOffset = _confirmedCore;
        MemoryOffset = _confirmedMem;
        _tuner.ApplyOffsets(SelectedDeviceIndex, _confirmedCore, _confirmedMem);
        ApplyStatus = "Auto-reverted (not confirmed).";
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            ApplyStatus = "Enter a profile name first.";
            return;
        }

        _profiles.Save(new OverclockProfile
        {
            Name = NewProfileName.Trim(),
            CoreOffsetMhz = CoreOffset,
            MemoryOffsetMhz = MemoryOffset,
        });
        RefreshProfiles();
        ApplyStatus = $"Saved profile \"{NewProfileName.Trim()}\".";
    }

    [RelayCommand]
    private void LoadProfile()
    {
        if (string.IsNullOrEmpty(SelectedProfile))
            return;
        var p = _profiles.TryGet(SelectedProfile);
        if (p is null)
            return;
        CoreOffset = p.CoreOffsetMhz;
        MemoryOffset = p.MemoryOffsetMhz;
        ApplyStatus = $"Loaded \"{p.Name}\" — click Apply to use it.";
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (string.IsNullOrEmpty(SelectedProfile))
            return;
        _profiles.Delete(SelectedProfile);
        RefreshProfiles();
    }

    private void RefreshProfiles()
    {
        ProfileNames.Clear();
        foreach (var name in _profiles.GetAll().Select(p => p.Name))
            ProfileNames.Add(name);
    }

    public void Dispose()
    {
        _revertTimer?.Stop();
        _tuner.Dispose();
    }
}
