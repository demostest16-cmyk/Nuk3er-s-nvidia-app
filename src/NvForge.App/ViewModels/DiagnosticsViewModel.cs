using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NvForge.App.Services;
using NvForge.Core.Models;

namespace NvForge.App.ViewModels;

/// <summary>Builds and exports the diagnostics bundle.</summary>
public partial class DiagnosticsViewModel : ObservableObject
{
    private readonly IReadOnlyList<GpuInfo> _gpus;
    private readonly string _source;
    private readonly bool _elevated;
    private readonly bool _simulated;
    private readonly string _appVersion;

    public DiagnosticsViewModel(
        IReadOnlyList<GpuInfo> gpus,
        string source,
        bool elevated,
        bool simulated,
        string appVersion)
    {
        _gpus = gpus;
        _source = source;
        _elevated = elevated;
        _simulated = simulated;
        _appVersion = appVersion;
        _reportText = BuildReport();
    }

    [ObservableProperty]
    private string _reportText;

    private string BuildReport() =>
        DiagnosticsReportBuilder.Build(_gpus, _source, _elevated, _simulated, _appVersion, DateTime.UtcNow);

    [RelayCommand]
    private void Refresh() => ReportText = BuildReport();

    [RelayCommand]
    private void Copy()
    {
        try { Clipboard.SetText(ReportText); }
        catch { /* clipboard can transiently fail; ignore */ }
    }

    [RelayCommand]
    private void Save()
    {
        var dialog = new SaveFileDialog
        {
            FileName = "nvforge-diagnostics.txt",
            Filter = "Text file (*.txt)|*.txt",
        };
        if (dialog.ShowDialog() == true)
            File.WriteAllText(dialog.FileName, ReportText);
    }
}
