using System.Diagnostics.CodeAnalysis;
using System.Windows;
using WindowsFileManager.ViewModels;

namespace WindowsFileManager.Views;

/// <summary>
/// Main window code-behind.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SaveSettings();
        }
    }
}
