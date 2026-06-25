using System.Windows;
using NvForge.App.ViewModels;

namespace NvForge.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as MainViewModel)?.Monitoring.Stop();
    }
}
