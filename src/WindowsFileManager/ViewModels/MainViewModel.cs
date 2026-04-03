using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;
using WindowsFileManager.Application.Services;
using WindowsFileManager.Core.Models;
using WindowsFileManager.Core.Services;
using WindowsFileManager.Helpers;
using WindowsFileManager.Infrastructure.Services;

namespace WindowsFileManager.ViewModels;

/// <summary>
/// ViewModel for the main duplicate file scanner window.
/// </summary>
[ExcludeFromCodeCoverage]
public class MainViewModel : ViewModelBase
{
    private readonly DuplicateScannerService _scannerService;
    private readonly SettingsService _settingsService;
    private bool _includeSubdirectories = true;
    private bool _isScanning;
    private int _filesScanned;
    private string _statusMessage = "Add folders and click Scan to find duplicate files.";
    private ScanResult? _lastResult;
    private ScanAnalytics? _analytics;
    private CancellationTokenSource? _cancellationTokenSource;
    private string _newFolderPath = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
        : this(CreateDefaultScanner(), CreateDefaultSettings())
    {
    }

    internal MainViewModel(DuplicateScannerService scannerService, SettingsService settingsService)
    {
        _scannerService = scannerService;
        _settingsService = settingsService;

        ScanCommand = new RelayCommand(_ => ScanAsync(), _ => CanScan());
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsScanning);
        AddFolderCommand = new RelayCommand(_ => AddFolder(), _ => !IsScanning);
        AddFolderByPathCommand = new RelayCommand(_ => AddFolderByPath(), _ => !IsScanning && !string.IsNullOrWhiteSpace(NewFolderPath));
        RemoveFolderCommand = new RelayCommand(p => RemoveFolder(p as string), _ => !IsScanning);
        OpenFileLocationCommand = new RelayCommand(p => OpenFileLocation(p as string));
        DeleteFileCommand = new RelayCommand(p => DeleteFile(p as ScannedFile), _ => !IsScanning);

        LoadSettings();
    }

    /// <summary>
    /// Gets the list of target folder paths.
    /// </summary>
    public ObservableCollection<string> TargetPaths { get; } = new();

    /// <summary>
    /// Gets or sets the new folder path typed in the input box.
    /// </summary>
    public string NewFolderPath
    {
        get => _newFolderPath;
        set => SetProperty(ref _newFolderPath, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include subdirectories.
    /// </summary>
    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set => SetProperty(ref _includeSubdirectories, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether a scan is in progress.
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    /// <summary>
    /// Gets or sets the number of files scanned so far.
    /// </summary>
    public int FilesScanned
    {
        get => _filesScanned;
        set => SetProperty(ref _filesScanned, value);
    }

    /// <summary>
    /// Gets or sets the status bar message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets the last scan result.
    /// </summary>
    public ScanResult? LastResult
    {
        get => _lastResult;
        set => SetProperty(ref _lastResult, value);
    }

    /// <summary>
    /// Gets or sets the analytics computed from the last scan.
    /// </summary>
    public ScanAnalytics? Analytics
    {
        get => _analytics;
        set => SetProperty(ref _analytics, value);
    }

    /// <summary>
    /// Gets the duplicate groups from the last scan.
    /// </summary>
    public ObservableCollection<DuplicateGroup> DuplicateGroups { get; } = new();

    /// <summary>
    /// Gets the scan command.
    /// </summary>
    public ICommand ScanCommand { get; }

    /// <summary>
    /// Gets the cancel command.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Gets the add folder command (browse dialog).
    /// </summary>
    public ICommand AddFolderCommand { get; }

    /// <summary>
    /// Gets the add folder by path command (text input).
    /// </summary>
    public ICommand AddFolderByPathCommand { get; }

    /// <summary>
    /// Gets the remove folder command.
    /// </summary>
    public ICommand RemoveFolderCommand { get; }

    /// <summary>
    /// Gets the open file location command.
    /// </summary>
    public ICommand OpenFileLocationCommand { get; }

    /// <summary>
    /// Gets the delete file command.
    /// </summary>
    public ICommand DeleteFileCommand { get; }

    /// <summary>
    /// Saves current settings to disk. Called on window close.
    /// </summary>
    public void SaveSettings()
    {
        var settings = new AppSettings
        {
            TargetPaths = TargetPaths.ToList(),
            IncludeSubdirectories = IncludeSubdirectories,
        };
        _settingsService.Save(settings);
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        IncludeSubdirectories = settings.IncludeSubdirectories;
        foreach (var path in settings.TargetPaths)
        {
            TargetPaths.Add(path);
        }
    }

    private bool CanScan() => !IsScanning && TargetPaths.Count > 0;

    private async void ScanAsync()
    {
        IsScanning = true;
        FilesScanned = 0;
        DuplicateGroups.Clear();
        Analytics = null;
        StatusMessage = "Scanning...";

        SaveSettings();

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var options = new ScanOptions
            {
                TargetPaths = TargetPaths.ToList(),
                IncludeSubdirectories = IncludeSubdirectories,
            };

            var result = await Task.Run(
                () => _scannerService.Scan(
                    options,
                    count => FilesScanned = count,
                    _cancellationTokenSource.Token),
                _cancellationTokenSource.Token);

            LastResult = result;
            Analytics = ScanAnalytics.FromResult(result);

            foreach (var group in result.DuplicateGroups)
            {
                DuplicateGroups.Add(group);
            }

            StatusMessage = $"Done — {result.TotalFilesScanned} files scanned, " +
                           $"{result.DuplicateGroups.Count} duplicate groups, " +
                           $"{result.FormattedWastedSize} wasted — " +
                           $"took {result.Duration.TotalSeconds:F1}s";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
        }
        catch (DirectoryNotFoundException ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsScanning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    private void AddFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select folder to scan for duplicates",
        };

        if (dialog.ShowDialog() == true && !TargetPaths.Contains(dialog.FolderName))
        {
            TargetPaths.Add(dialog.FolderName);
        }
    }

    private void AddFolderByPath()
    {
        var path = NewFolderPath.Trim();
        if (!string.IsNullOrEmpty(path) && !TargetPaths.Contains(path))
        {
            TargetPaths.Add(path);
            NewFolderPath = string.Empty;
        }
    }

    private void RemoveFolder(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            TargetPaths.Remove(path);
        }
    }

    private static void OpenFileLocation(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
    }

    private void DeleteFile(ScannedFile? file)
    {
        if (file == null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Delete this file?\n\n{file.FilePath}",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            File.Delete(file.FilePath);

            foreach (var group in DuplicateGroups.ToList())
            {
                if (group.Files.Remove(file))
                {
                    if (group.Files.Count <= 1)
                    {
                        DuplicateGroups.Remove(group);
                    }

                    break;
                }
            }

            StatusMessage = $"Deleted: {file.FilePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to delete: {ex.Message}";
        }
    }

    private static DuplicateScannerService CreateDefaultScanner()
    {
        var fs = new FileSystemService();
        return new DuplicateScannerService(fs, new FileHashService(fs));
    }

    private static SettingsService CreateDefaultSettings()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsFileManager",
            "settings.json");
        return new SettingsService(new FileSystemService(), settingsPath);
    }
}
