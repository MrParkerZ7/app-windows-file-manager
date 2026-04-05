using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
    private string _previewType = "none";
    private BitmapImage? _previewImage;
    private Uri? _previewMediaUri;
    private string? _previewText;
    private string? _previewFileName;
    private string? _previewFileSize;
    private bool _isPreviewVisible;
    private long _minFileSizeBytes;
    private string _minFileSizeText = string.Empty;
    private string _selectedSizeUnit = "KB";
    private bool _isFilterVisible;
    private int _filteredGroupCount;
    private int _totalGroupCount;
    private bool _isAutoPreview = true;
    private bool _isAutoPlay;
    private bool _isMiniPreview = true;
    private double _mediaVolume = 0.5;
    private string _resourceMemory = string.Empty;
    private string _resourceCpu = string.Empty;
    private string _resourceThreads = string.Empty;
    private int _selectedGroupCount;
    private bool _isSelectAll;
    private string _moveTargetPath = string.Empty;
    private string _moveFilterText = string.Empty;
    private bool _isActionsVisible;
    private readonly DispatcherTimer _resourceTimer;
    private readonly Process _currentProcess;
    private TimeSpan _lastCpuTime;
    private DateTime _lastCheckTime;
    private DuplicateGroup? _selectedDuplicateGroup;
    private string _selectedSortOption = "Size (largest)";
    private int _minDuplicateCount = 2;

    private static readonly List<string> SortOptionsList = new()
    {
        "Size (largest)",
        "Size (smallest)",
        "File count (most)",
        "File count (fewest)",
        "Wasted space (most)",
        "Wasted space (least)",
        "Type (A-Z)",
        "Type (Z-A)",
        "Name (A-Z)",
        "Name (Z-A)",
    };

    private static readonly HashSet<string> ImageExtensions =
    [

        // Common raster
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".gif",
        ".tiff",
        ".tif",
        ".ico",
        ".webp",
        ".jfif",
        ".jif",
        ".jpe",
        ".dib",
        ".wdp",
        ".hdp",
        ".jxr",

        // Raw camera
        ".raw",
        ".cr2",
        ".cr3",
        ".nef",
        ".arw",
        ".dng",
        ".orf",
        ".rw2",
        ".pef",
        ".srw",
        ".raf",

        // Vector / design
        ".svg",
        ".svgz",

        // Other
        ".heic",
        ".heif",
        ".avif",
        ".tga",
        ".pcx",
        ".pbm",
        ".pgm",
        ".ppm",
        ".pnm",
        ".exr",
        ".hdr",
        ".cur",
        ".ani",
    ];

    private static readonly HashSet<string> VideoExtensions =
    [

        // Common
        ".mp4",
        ".avi",
        ".mkv",
        ".wmv",
        ".mov",
        ".flv",
        ".webm",
        ".m4v",
        ".mpg",
        ".mpeg",

        // Extended
        ".3gp",
        ".3g2",
        ".ts",
        ".mts",
        ".m2ts",
        ".vob",
        ".ogv",
        ".divx",
        ".xvid",
        ".asf",
        ".rm",
        ".rmvb",
        ".f4v",
        ".swf",
        ".amv",
        ".mxf",
        ".dv",
        ".m2v",
        ".m4p",
        ".mp2",
        ".mpe",
        ".mpv",
    ];

    private static readonly HashSet<string> AudioExtensions =
    [

        // Common
        ".mp3",
        ".wav",
        ".flac",
        ".aac",
        ".ogg",
        ".wma",
        ".m4a",
        ".opus",

        // Extended
        ".aiff",
        ".aif",
        ".aifc",
        ".alac",
        ".ape",
        ".dsf",
        ".dff",
        ".mid",
        ".midi",
        ".kar",
        ".mka",
        ".oga",
        ".pcm",
        ".ra",
        ".ram",
        ".wv",
        ".ac3",
        ".dts",
        ".amr",
        ".awb",
        ".au",
        ".snd",
        ".caf",
        ".tak",
        ".tta",
        ".shn",
        ".spx",
        ".gsm",
        ".mp2",
        ".mpa",
        ".m3u",
        ".m3u8",
        ".pls",
        ".wpl",
        ".cue",
    ];

    private static readonly HashSet<string> TextExtensions =
    [

        // Plain text / data
        ".txt",
        ".log",
        ".csv",
        ".tsv",
        ".tab",
        ".json",
        ".jsonl",
        ".json5",
        ".xml",
        ".xsl",
        ".xslt",
        ".xsd",
        ".dtd",
        ".yaml",
        ".yml",
        ".toml",
        ".ini",
        ".cfg",
        ".conf",
        ".config",
        ".properties",
        ".env",
        ".env.local",
        ".env.example",
        ".editorconfig",
        ".gitignore",
        ".gitattributes",
        ".dockerignore",
        ".npmrc",
        ".nvmrc",
        ".eslintrc",
        ".prettierrc",
        ".babelrc",

        // Markdown / docs
        ".md",
        ".mdx",
        ".rst",
        ".tex",
        ".latex",
        ".bib",
        ".adoc",
        ".asciidoc",
        ".textile",
        ".wiki",
        ".nfo",
        ".diz",
        ".ans",

        // C / C++ / Obj-C
        ".c",
        ".h",
        ".cpp",
        ".cxx",
        ".cc",
        ".hpp",
        ".hxx",
        ".hh",
        ".m",
        ".mm",

        // C# / .NET
        ".cs",
        ".csx",
        ".fs",
        ".fsx",
        ".fsi",
        ".vb",
        ".xaml",
        ".cshtml",
        ".razor",
        ".csproj",
        ".sln",
        ".props",
        ".targets",
        ".resx",
        ".designer.cs",

        // Java / JVM
        ".java",
        ".kt",
        ".kts",
        ".groovy",
        ".gradle",
        ".scala",
        ".clj",
        ".cljs",
        ".edn",

        // JavaScript / TypeScript / Web
        ".js",
        ".jsx",
        ".ts",
        ".tsx",
        ".mjs",
        ".cjs",
        ".vue",
        ".svelte",
        ".astro",
        ".html",
        ".htm",
        ".xhtml",
        ".css",
        ".scss",
        ".sass",
        ".less",
        ".styl",
        ".stylus",

        // Python
        ".py",
        ".pyw",
        ".pyi",
        ".pyx",
        ".pxd",
        ".pxi",
        ".pyproj",
        ".pip",
        ".pipfile",

        // Ruby
        ".rb",
        ".erb",
        ".rake",
        ".gemspec",
        ".gemfile",

        // PHP
        ".php",
        ".phtml",
        ".php3",
        ".php4",
        ".php5",
        ".phps",
        ".blade.php",

        // Go
        ".go",
        ".mod",
        ".sum",

        // Rust
        ".rs",
        ".toml",

        // Swift / Kotlin
        ".swift",
        ".playground",

        // Shell / scripting
        ".sh",
        ".bash",
        ".zsh",
        ".fish",
        ".ksh",
        ".csh",
        ".tcsh",
        ".bat",
        ".cmd",
        ".ps1",
        ".psm1",
        ".psd1",
        ".awk",
        ".sed",

        // SQL / DB
        ".sql",
        ".plsql",
        ".pgsql",
        ".mysql",
        ".sqlite",
        ".ddl",
        ".dml",

        // Functional / academic
        ".hs",
        ".lhs",
        ".erl",
        ".hrl",
        ".ex",
        ".exs",
        ".elm",
        ".ml",
        ".mli",
        ".ocaml",
        ".lisp",
        ".cl",
        ".el",
        ".scm",
        ".rkt",

        // Lua / Perl / R / Julia / Dart
        ".lua",
        ".pl",
        ".pm",
        ".t",
        ".r",
        ".rmd",
        ".jl",
        ".dart",

        // DevOps / config
        ".tf",
        ".tfvars",
        ".hcl",
        ".vagrant",
        ".ansible",
        ".dockerfile",
        ".makefile",
        ".cmake",
        ".ninja",

        // Data / serialization
        ".proto",
        ".thrift",
        ".avsc",
        ".graphql",
        ".gql",
        ".csv",
        ".ics",
        ".vcf",
        ".ldif",

        // Assembly / low-level
        ".asm",
        ".s",
        ".nasm",
        ".masm",

        // Misc dev
        ".diff",
        ".patch",
        ".reg",
        ".inf",
        ".manifest",
        ".srt",
        ".sub",
        ".ssa",
        ".ass",
        ".vtt",
        ".lrc",
    ];

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
        DeleteAllInGroupCommand = new RelayCommand(p => DeleteAllInGroup(p as DuplicateGroup), _ => !IsScanning);
        PreviewFileCommand = new RelayCommand(p => PreviewFile(p as string));
        ClosePreviewCommand = new RelayCommand(_ => ClosePreview());
        ShowAllTypesCommand = new RelayCommand(_ => SetAllExtensions(true));
        ClearAllTypesCommand = new RelayCommand(_ => SetAllExtensions(false));
        ApplyFileSizeFilterCommand = new RelayCommand(_ => ApplyFilters());
        ToggleFilterCommand = new RelayCommand(_ => IsFilterVisible = !IsFilterVisible);
        ToggleActionsCommand = new RelayCommand(_ => IsActionsVisible = !IsActionsVisible);
        BulkDeleteKeepNewestCommand = new RelayCommand(_ => BulkDeleteKeepNewest(), _ => SelectedGroupCount > 0);
        BulkDeleteKeepOldestCommand = new RelayCommand(_ => BulkDeleteKeepOldest(), _ => SelectedGroupCount > 0);
        BulkDeleteAllCommand = new RelayCommand(_ => BulkDeleteAll(), _ => SelectedGroupCount > 0);
        BulkMoveToCommand = new RelayCommand(_ => BulkMoveTo(), _ => SelectedGroupCount > 0);
        BulkMoveOldestCommand = new RelayCommand(_ => BulkMoveOldest(), _ => SelectedGroupCount > 0);
        BulkMoveNewestCommand = new RelayCommand(_ => BulkMoveNewest(), _ => SelectedGroupCount > 0);
        BulkMoveByFilenameCommand = new RelayCommand(_ => BulkMoveByFilename(), _ => SelectedGroupCount > 0);
        BulkMoveByPathCommand = new RelayCommand(_ => BulkMoveByPath(), _ => SelectedGroupCount > 0);
        BrowseMoveTargetCommand = new RelayCommand(_ => BrowseMoveTarget());

        FilteredDuplicateGroups = CollectionViewSource.GetDefaultView(DuplicateGroups);
        FilteredDuplicateGroups.Filter = FilterDuplicateGroup;

        LoadSettings();

        // Resource monitor
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuTime = _currentProcess.TotalProcessorTime;
        _lastCheckTime = DateTime.UtcNow;
        UpdateResourceInfo();

        _resourceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _resourceTimer.Tick += (_, _) => UpdateResourceInfo();
        _resourceTimer.Start();
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
    /// Gets the delete all files in group command.
    /// </summary>
    public ICommand DeleteAllInGroupCommand { get; }

    /// <summary>
    /// Gets the preview file command.
    /// </summary>
    public ICommand PreviewFileCommand { get; }

    /// <summary>
    /// Gets the close preview command.
    /// </summary>
    public ICommand ClosePreviewCommand { get; }

    /// <summary>
    /// Gets the show all types command.
    /// </summary>
    public ICommand ShowAllTypesCommand { get; }

    /// <summary>
    /// Gets the clear all types command.
    /// </summary>
    public ICommand ClearAllTypesCommand { get; }

    /// <summary>
    /// Gets the apply file size filter command.
    /// </summary>
    public ICommand ApplyFileSizeFilterCommand { get; }

    /// <summary>
    /// Gets the toggle filter panel command.
    /// </summary>
    public ICommand ToggleFilterCommand { get; }

    /// <summary>
    /// Gets the filtered view of duplicate groups.
    /// </summary>
    public ICollectionView FilteredDuplicateGroups { get; }

    /// <summary>
    /// Gets the available extension filters.
    /// </summary>
    public ObservableCollection<ExtensionFilter> ExtensionFilters { get; } = new();

    /// <summary>
    /// Gets the available size unit options.
    /// </summary>
    public List<string> SizeUnits { get; } = new() { "B", "KB", "MB", "GB" };

    /// <summary>
    /// Gets or sets a value indicating whether the filter panel is visible.
    /// </summary>
    public bool IsFilterVisible
    {
        get => _isFilterVisible;
        set => SetProperty(ref _isFilterVisible, value);
    }

    /// <summary>
    /// Gets or sets the minimum file size text input.
    /// </summary>
    public string MinFileSizeText
    {
        get => _minFileSizeText;
        set => SetProperty(ref _minFileSizeText, value);
    }

    /// <summary>
    /// Gets or sets the selected size unit.
    /// </summary>
    public string SelectedSizeUnit
    {
        get => _selectedSizeUnit;
        set => SetProperty(ref _selectedSizeUnit, value);
    }

    /// <summary>
    /// Gets or sets the minimum duplicate count filter.
    /// </summary>
    public int MinDuplicateCount
    {
        get => _minDuplicateCount;
        set => SetProperty(ref _minDuplicateCount, value);
    }

    /// <summary>
    /// Gets or sets the filtered group count display.
    /// </summary>
    public int FilteredGroupCount
    {
        get => _filteredGroupCount;
        set => SetProperty(ref _filteredGroupCount, value);
    }

    /// <summary>
    /// Gets or sets the total group count.
    /// </summary>
    public int TotalGroupCount
    {
        get => _totalGroupCount;
        set => SetProperty(ref _totalGroupCount, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether preview is enabled on selection.
    /// </summary>
    public bool IsAutoPreview
    {
        get => _isAutoPreview;
        set => SetProperty(ref _isAutoPreview, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether mini preview is shown in the list.
    /// </summary>
    public bool IsMiniPreview
    {
        get => _isMiniPreview;
        set => SetProperty(ref _isMiniPreview, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether video/audio auto-plays.
    /// </summary>
    public bool IsAutoPlay
    {
        get => _isAutoPlay;
        set => SetProperty(ref _isAutoPlay, value);
    }

    /// <summary>
    /// Gets or sets the media volume (0.0 to 1.0).
    /// </summary>
    public double MediaVolume
    {
        get => _mediaVolume;
        set => SetProperty(ref _mediaVolume, value);
    }

    /// <summary>
    /// Gets or sets the memory usage display.
    /// </summary>
    public string ResourceMemory
    {
        get => _resourceMemory;
        set => SetProperty(ref _resourceMemory, value);
    }

    /// <summary>
    /// Gets or sets the CPU usage display.
    /// </summary>
    public string ResourceCpu
    {
        get => _resourceCpu;
        set => SetProperty(ref _resourceCpu, value);
    }

    /// <summary>
    /// Gets or sets the thread count display.
    /// </summary>
    public string ResourceThreads
    {
        get => _resourceThreads;
        set => SetProperty(ref _resourceThreads, value);
    }

    /// <summary>
    /// Gets or sets the number of selected groups.
    /// </summary>
    public int SelectedGroupCount
    {
        get => _selectedGroupCount;
        set
        {
            SetProperty(ref _selectedGroupCount, value);
            OnPropertyChanged(nameof(HasSelectedGroups));
        }
    }

    /// <summary>
    /// Gets a value indicating whether any groups are selected.
    /// </summary>
    public bool HasSelectedGroups => SelectedGroupCount > 0;

    /// <summary>
    /// Gets or sets a value indicating whether select all is active.
    /// </summary>
    public bool IsSelectAll
    {
        get => _isSelectAll;
        set => SetProperty(ref _isSelectAll, value);
    }

    /// <summary>
    /// Gets or sets the move target path.
    /// </summary>
    public string MoveTargetPath
    {
        get => _moveTargetPath;
        set => SetProperty(ref _moveTargetPath, value);
    }

    /// <summary>
    /// Gets the selected groups (computed from IsSelected on each group).
    /// </summary>
    public List<DuplicateGroup> SelectedGroups =>
        DuplicateGroups.Where(g => g.IsSelected).ToList();

    /// <summary>
    /// Gets or sets a value indicating whether the actions panel is expanded.
    /// </summary>
    public bool IsActionsVisible
    {
        get => _isActionsVisible;
        set => SetProperty(ref _isActionsVisible, value);
    }

    /// <summary>Gets the toggle actions panel command.</summary>
    public ICommand ToggleActionsCommand { get; }

    /// <summary>Gets the bulk delete keep newest command.</summary>
    public ICommand BulkDeleteKeepNewestCommand { get; }

    /// <summary>Gets the bulk delete keep oldest command.</summary>
    public ICommand BulkDeleteKeepOldestCommand { get; }

    /// <summary>Gets the bulk delete all command.</summary>
    public ICommand BulkDeleteAllCommand { get; }

    /// <summary>Gets the bulk move to command.</summary>
    public ICommand BulkMoveToCommand { get; }

    /// <summary>Gets the bulk move oldest files command.</summary>
    public ICommand BulkMoveOldestCommand { get; }

    /// <summary>Gets the bulk move newest files command.</summary>
    public ICommand BulkMoveNewestCommand { get; }

    /// <summary>Gets the bulk move by filename contains command.</summary>
    public ICommand BulkMoveByFilenameCommand { get; }

    /// <summary>Gets the bulk move by path contains command.</summary>
    public ICommand BulkMoveByPathCommand { get; }

    /// <summary>Gets the browse move target command.</summary>
    public ICommand BrowseMoveTargetCommand { get; }

    /// <summary>
    /// Gets or sets the move filter text for filename/path contains filtering.
    /// </summary>
    public string MoveFilterText
    {
        get => _moveFilterText;
        set => SetProperty(ref _moveFilterText, value);
    }

    /// <summary>
    /// Gets the sort options list.
    /// </summary>
    public List<string> SortOptions => SortOptionsList;

    /// <summary>
    /// Gets or sets the selected sort option.
    /// </summary>
    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (SetProperty(ref _selectedSortOption, value))
            {
                ApplySorting();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected duplicate group.
    /// </summary>
    public DuplicateGroup? SelectedDuplicateGroup
    {
        get => _selectedDuplicateGroup;
        set
        {
            if (SetProperty(ref _selectedDuplicateGroup, value) && value != null && IsAutoPreview)
            {
                PreviewFile(value.FirstFilePath);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the preview panel is visible.
    /// </summary>
    public bool IsPreviewVisible
    {
        get => _isPreviewVisible;
        set => SetProperty(ref _isPreviewVisible, value);
    }

    /// <summary>
    /// Gets or sets the preview type: "image", "video", "audio", "pdf", "text", or "none".
    /// </summary>
    public string PreviewType
    {
        get => _previewType;
        set => SetProperty(ref _previewType, value);
    }

    /// <summary>
    /// Gets or sets the preview image source.
    /// </summary>
    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        set => SetProperty(ref _previewImage, value);
    }

    /// <summary>
    /// Gets or sets the media URI for video/audio preview.
    /// </summary>
    public Uri? PreviewMediaUri
    {
        get => _previewMediaUri;
        set => SetProperty(ref _previewMediaUri, value);
    }

    /// <summary>
    /// Gets or sets the text content for text file preview.
    /// </summary>
    public string? PreviewText
    {
        get => _previewText;
        set => SetProperty(ref _previewText, value);
    }

    /// <summary>
    /// Gets or sets the preview file name.
    /// </summary>
    public string? PreviewFileName
    {
        get => _previewFileName;
        set => SetProperty(ref _previewFileName, value);
    }

    /// <summary>
    /// Gets or sets the preview file size.
    /// </summary>
    public string? PreviewFileSize
    {
        get => _previewFileSize;
        set => SetProperty(ref _previewFileSize, value);
    }

    /// <summary>
    /// Saves current settings to disk. Called on window close.
    /// </summary>
    public void SaveSettings()
    {
        var settings = new AppSettings
        {
            TargetPaths = TargetPaths.ToList(),
            IncludeSubdirectories = IncludeSubdirectories,
            IsMiniPreview = IsMiniPreview,
            IsAutoPreview = IsAutoPreview,
            IsAutoPlay = IsAutoPlay,
            SelectedSortOption = SelectedSortOption,
            Volume = MediaVolume,
            MoveTargetPath = MoveTargetPath,
        };
        _settingsService.Save(settings);
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        IncludeSubdirectories = settings.IncludeSubdirectories;
        IsMiniPreview = settings.IsMiniPreview;
        IsAutoPreview = settings.IsAutoPreview;
        IsAutoPlay = settings.IsAutoPlay;
        SelectedSortOption = settings.SelectedSortOption;
        MediaVolume = settings.Volume;
        MoveTargetPath = settings.MoveTargetPath;
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
        MiniPreviewConverter.ClearCache();
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

            BuildExtensionFilters(result.DuplicateGroups);
            TotalGroupCount = result.DuplicateGroups.Count;
            FilteredGroupCount = result.DuplicateGroups.Count;

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

    private void DeleteAllInGroup(DuplicateGroup? group)
    {
        if (group == null || group.Files.Count == 0)
        {
            return;
        }

        var fileList = string.Join("\n", group.Files.Select(f => f.FilePath));
        var result = System.Windows.MessageBox.Show(
            $"Delete ALL {group.Files.Count} files in this group?\n\n{fileList}",
            "Confirm Delete All",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = 0;
        var failed = 0;

        foreach (var file in group.Files.ToList())
        {
            try
            {
                File.Delete(file.FilePath);
                deleted++;
            }
            catch
            {
                failed++;
            }
        }

        DuplicateGroups.Remove(group);
        ClosePreview();

        StatusMessage = failed == 0
            ? $"Deleted all {deleted} files in group"
            : $"Deleted {deleted} files, {failed} failed";
    }

    private void BuildExtensionFilters(IReadOnlyList<DuplicateGroup> groups)
    {
        ExtensionFilters.Clear();

        var extStats = groups
            .GroupBy(g => Path.GetExtension(g.Files[0].FilePath).ToLowerInvariant())
            .Select(g => new ExtensionFilter
            {
                Extension = string.IsNullOrEmpty(g.Key) ? "(no ext)" : g.Key,
                IsChecked = true,
                FileCount = g.Count(),
                TotalSize = g.Sum(x => x.FileSize * x.Count),
            })
            .OrderByDescending(e => e.FileCount)
            .ToList();

        foreach (var ext in extStats)
        {
            ext.PropertyChanged += (_, _) => ApplyFilters();
            ExtensionFilters.Add(ext);
        }
    }

    private void SetAllExtensions(bool isChecked)
    {
        foreach (var ext in ExtensionFilters)
        {
            ext.IsChecked = isChecked;
        }
    }

    private void ApplyFilters()
    {
        // Parse min file size
        _minFileSizeBytes = 0;
        if (double.TryParse(MinFileSizeText, out var sizeVal) && sizeVal > 0)
        {
            _minFileSizeBytes = SelectedSizeUnit switch
            {
                "B" => (long)sizeVal,
                "KB" => (long)(sizeVal * 1024),
                "MB" => (long)(sizeVal * 1024 * 1024),
                "GB" => (long)(sizeVal * 1024 * 1024 * 1024),
                _ => 0,
            };
        }

        FilteredDuplicateGroups.Refresh();

        // Update count
        var count = 0;
        foreach (var item in FilteredDuplicateGroups)
        {
            count++;
        }

        FilteredGroupCount = count;
    }

    private void ApplySorting()
    {
        FilteredDuplicateGroups.SortDescriptions.Clear();

        switch (SelectedSortOption)
        {
            case "Size (largest)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.FileSize), ListSortDirection.Descending));
                break;
            case "Size (smallest)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.FileSize), ListSortDirection.Ascending));
                break;
            case "File count (most)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.Count), ListSortDirection.Descending));
                break;
            case "File count (fewest)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.Count), ListSortDirection.Ascending));
                break;
            case "Wasted space (most)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.WastedBytes), ListSortDirection.Descending));
                break;
            case "Wasted space (least)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.WastedBytes), ListSortDirection.Ascending));
                break;
            case "Type (A-Z)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.FileExtension), ListSortDirection.Ascending));
                break;
            case "Type (Z-A)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.FileExtension), ListSortDirection.Descending));
                break;
            case "Name (A-Z)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.FirstFileName), ListSortDirection.Ascending));
                break;
            case "Name (Z-A)":
                FilteredDuplicateGroups.SortDescriptions.Add(
                    new SortDescription(nameof(DuplicateGroup.FirstFileName), ListSortDirection.Descending));
                break;
        }

        FilteredDuplicateGroups.Refresh();
    }

    private bool FilterDuplicateGroup(object obj)
    {
        if (obj is not DuplicateGroup group)
        {
            return false;
        }

        // Check duplicate count filter
        if (group.Count < _minDuplicateCount)
        {
            return false;
        }

        // Check file size filter
        if (_minFileSizeBytes > 0 && group.FileSize < _minFileSizeBytes)
        {
            return false;
        }

        // Check extension filter
        if (ExtensionFilters.Count > 0)
        {
            var ext = Path.GetExtension(group.Files[0].FilePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext))
            {
                ext = "(no ext)";
            }

            var filter = ExtensionFilters.FirstOrDefault(f => f.Extension == ext);
            if (filter != null && !filter.IsChecked)
            {
                return false;
            }
        }

        return true;
    }

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // PDF
        ".pdf",

        // Microsoft Office
        ".doc", ".docx", ".docm", ".dot", ".dotx", ".dotm",
        ".xls", ".xlsx", ".xlsm", ".xlsb", ".xlt", ".xltx", ".xltm",
        ".ppt", ".pptx", ".pptm", ".pot", ".potx", ".potm", ".pps", ".ppsx",
        ".one", ".onetoc2", ".vsd", ".vsdx", ".pub", ".mpp",
        ".accdb", ".accde", ".mdb",

        // LibreOffice / OpenDocument
        ".odt", ".ods", ".odp", ".odg", ".odf", ".odb", ".odc",

        // Apple
        ".pages", ".numbers", ".keynote",

        // eBooks
        ".epub", ".mobi", ".azw", ".azw3", ".fb2", ".djvu", ".cbz", ".cbr",

        // Other docs
        ".rtf", ".wps", ".wpd", ".abw", ".xps", ".oxps",
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".xz", ".lz", ".lzma",
        ".zst", ".br", ".cab", ".iso", ".img", ".dmg", ".vhd", ".vhdx", ".vmdk",
        ".wim", ".jar", ".war", ".ear", ".apk", ".aab", ".ipa", ".deb", ".rpm",
        ".snap", ".flatpak", ".appimage", ".msi", ".msix", ".msixbundle",
        ".nupkg", ".crate", ".gem", ".egg", ".whl",
        ".pak", ".pkg", ".arc", ".lzh", ".arj", ".ace",
    };

    private static readonly HashSet<string> FontExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ttf", ".otf", ".woff", ".woff2", ".eot", ".ttc", ".fon", ".fnt", ".pfb", ".pfm",
    };

    private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".sys", ".drv", ".ocx", ".com", ".scr",
        ".so", ".dylib", ".a", ".lib", ".o", ".obj",
        ".class", ".pyc", ".pyo", ".pdb", ".ilk", ".exp",
    };

    private static readonly HashSet<string> DatabaseExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".db", ".sqlite", ".sqlite3", ".mdf", ".ldf", ".ndf", ".bak", ".dbf",
        ".fdb", ".gdb", ".ibd", ".frm", ".myd", ".myi",
    };

    private static readonly HashSet<string> ThreeDModelExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".obj", ".fbx", ".gltf", ".glb", ".stl", ".dae", ".3ds", ".blend", ".max",
        ".c4d", ".ma", ".mb", ".ply", ".usd", ".usda", ".usdz",
    };

    private static readonly Dictionary<string, string> CategoryIcons = new()
    {
        { "document", "📄" },
        { "archive", "📦" },
        { "font", "🔤" },
        { "executable", "⚙️" },
        { "database", "🗄️" },
        { "3dmodel", "🧊" },
    };

    private void PreviewFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return;
        }

        // Clear previous preview
        PreviewImage = null;
        PreviewMediaUri = null;
        PreviewText = null;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var fileInfo = new FileInfo(filePath);
        PreviewFileName = fileInfo.Name;
        PreviewFileSize = ScannedFile.FormatFileSize(fileInfo.Length);

        if (ImageExtensions.Contains(ext))
        {
            // Only WPF-native image formats can be loaded as BitmapImage
            if (IsWpfNativeImage(ext))
            {
                PreviewType = "image";
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 600;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage = bitmap;
                }
                catch
                {
                    PreviewType = "unsupported";
                    PreviewText = "Image format not supported by WPF decoder";
                }
            }
            else
            {
                SetInfoCardPreview("image", "🖼️", "Image File", ext);
            }
        }
        else if (VideoExtensions.Contains(ext))
        {
            PreviewType = "video";
            PreviewMediaUri = new Uri(filePath);
        }
        else if (AudioExtensions.Contains(ext))
        {
            PreviewType = "audio";
            PreviewMediaUri = new Uri(filePath);
        }
        else if (DocumentExtensions.Contains(ext))
        {
            SetInfoCardPreview("document", CategoryIcons["document"], "Document", ext);
        }
        else if (ArchiveExtensions.Contains(ext))
        {
            SetInfoCardPreview("archive", CategoryIcons["archive"], "Archive / Package", ext);
        }
        else if (FontExtensions.Contains(ext))
        {
            SetInfoCardPreview("font", CategoryIcons["font"], "Font File", ext);
        }
        else if (ExecutableExtensions.Contains(ext))
        {
            SetInfoCardPreview("executable", CategoryIcons["executable"], "Executable / Binary", ext);
        }
        else if (DatabaseExtensions.Contains(ext))
        {
            SetInfoCardPreview("database", CategoryIcons["database"], "Database File", ext);
        }
        else if (ThreeDModelExtensions.Contains(ext))
        {
            SetInfoCardPreview("3dmodel", CategoryIcons["3dmodel"], "3D Model", ext);
        }
        else if (TextExtensions.Contains(ext) || TryReadAsText(filePath))
        {
            PreviewType = "text";
            if (PreviewText == null)
            {
                ReadTextPreview(filePath);
            }
        }
        else
        {
            PreviewType = "unsupported";
        }

        IsPreviewVisible = true;
    }

    private void SetInfoCardPreview(string type, string icon, string label, string ext)
    {
        PreviewType = "infocard";
        PreviewText = $"{icon}\n{label}\n{ext.ToUpperInvariant()}\nUse 'Open' to view in default app";
    }

    private static bool IsWpfNativeImage(string ext)
    {
        return ext is ".jpg" or ".jpeg" or ".jif" or ".jfif" or ".jpe"
            or ".png" or ".bmp" or ".dib" or ".gif"
            or ".tiff" or ".tif" or ".ico" or ".wdp" or ".hdp" or ".jxr";
    }

    private bool TryReadAsText(string filePath)
    {
        // Try to detect if unknown file is actually text
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[8192];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                return false;
            }

            // Check for null bytes (binary indicator)
            var nullCount = 0;
            for (var i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 0)
                {
                    nullCount++;
                }
            }

            // If less than 1% null bytes, likely text
            if (nullCount * 100 / bytesRead < 1)
            {
                ReadTextPreview(filePath);
                return true;
            }
        }
        catch
        {
            // Not readable
        }

        return false;
    }

    private void ReadTextPreview(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var buffer = new char[50_000];
            var charsRead = reader.Read(buffer, 0, buffer.Length);
            PreviewText = new string(buffer, 0, charsRead);
            if (charsRead == buffer.Length)
            {
                PreviewText += "\n\n--- [Preview truncated] ---";
            }
        }
        catch
        {
            PreviewText = "[Unable to read file]";
        }
    }

    private void ClosePreview()
    {
        IsPreviewVisible = false;
        IsAutoPreview = false;
        PreviewType = "none";
        PreviewImage = null;
        PreviewMediaUri = null;
        PreviewText = null;
        PreviewFileName = null;
        PreviewFileSize = null;
    }

    private void BulkDeleteKeepNewest()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0)
        {
            return;
        }

        var totalFiles = groups.Sum(g => g.Count - 1);
        var result = System.Windows.MessageBox.Show(
            $"Delete older duplicates in {groups.Count} groups?\n" +
            $"Will keep the newest file in each group.\n" +
            $"{totalFiles} files will be deleted.",
            "Confirm Bulk Delete (Keep Newest)",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = 0;
        var failed = 0;

        foreach (var group in groups)
        {
            var newest = group.Files.OrderByDescending(f => f.LastModified).First();
            foreach (var file in group.Files.Where(f => f != newest).ToList())
            {
                try
                {
                    File.Delete(file.FilePath);
                    group.Files.Remove(file);
                    deleted++;
                }
                catch
                {
                    failed++;
                }
            }

            if (group.Files.Count <= 1)
            {
                DuplicateGroups.Remove(group);
            }
        }

        StatusMessage = $"Bulk delete: {deleted} older files removed" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void BulkDeleteKeepOldest()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0)
        {
            return;
        }

        var totalFiles = groups.Sum(g => g.Count - 1);
        var result = System.Windows.MessageBox.Show(
            $"Delete newer duplicates in {groups.Count} groups?\n" +
            $"Will keep the oldest file in each group.\n" +
            $"{totalFiles} files will be deleted.",
            "Confirm Bulk Delete (Keep Oldest)",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = 0;
        var failed = 0;

        foreach (var group in groups)
        {
            var oldest = group.Files.OrderBy(f => f.LastModified).First();
            foreach (var file in group.Files.Where(f => f != oldest).ToList())
            {
                try
                {
                    File.Delete(file.FilePath);
                    group.Files.Remove(file);
                    deleted++;
                }
                catch
                {
                    failed++;
                }
            }

            if (group.Files.Count <= 1)
            {
                DuplicateGroups.Remove(group);
            }
        }

        StatusMessage = $"Bulk delete: {deleted} newer files removed" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void BulkDeleteAll()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0)
        {
            return;
        }

        var totalFiles = groups.Sum(g => g.Count);
        var result = System.Windows.MessageBox.Show(
            $"Delete ALL files in {groups.Count} groups?\n" +
            $"{totalFiles} files will be permanently deleted!",
            "Confirm Bulk Delete All",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = 0;
        var failed = 0;

        foreach (var group in groups)
        {
            foreach (var file in group.Files.ToList())
            {
                try
                {
                    File.Delete(file.FilePath);
                    deleted++;
                }
                catch
                {
                    failed++;
                }
            }

            DuplicateGroups.Remove(group);
        }

        ClosePreview();
        StatusMessage = $"Bulk delete: {deleted} files removed" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void BulkMoveTo()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0 || string.IsNullOrWhiteSpace(MoveTargetPath))
        {
            return;
        }

        if (!EnsureMoveTargetDirectory())
        {
            return;
        }

        var totalFiles = groups.Sum(g => g.Count);
        var result = System.Windows.MessageBox.Show(
            $"Move ALL {totalFiles} files from {groups.Count} groups to:\n{MoveTargetPath}",
            "Confirm Bulk Move",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var moved = 0;
        var failed = 0;

        foreach (var group in groups)
        {
            foreach (var file in group.Files.ToList())
            {
                if (MoveFileToTarget(file.FilePath))
                {
                    moved++;
                }
                else
                {
                    failed++;
                }
            }

            DuplicateGroups.Remove(group);
        }

        ClosePreview();
        StatusMessage = $"Bulk move: {moved} files moved to {MoveTargetPath}" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void BrowseMoveTarget()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select folder to move files to",
        };

        if (dialog.ShowDialog() == true)
        {
            MoveTargetPath = dialog.FolderName;
        }
    }

    private void BulkMoveOldest()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0 || string.IsNullOrWhiteSpace(MoveTargetPath))
        {
            return;
        }

        if (!EnsureMoveTargetDirectory())
        {
            return;
        }

        var totalFiles = groups.Sum(g => g.Count - 1);
        var result = System.Windows.MessageBox.Show(
            $"Move older duplicates from {groups.Count} groups to:\n{MoveTargetPath}\n" +
            $"Will keep the newest file in each group.\n{totalFiles} files will be moved.",
            "Confirm Move Oldest",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var moved = 0;
        var failed = 0;

        foreach (var group in groups)
        {
            var newest = group.Files.OrderByDescending(f => f.LastModified).First();
            foreach (var file in group.Files.Where(f => f != newest).ToList())
            {
                if (MoveFileToTarget(file.FilePath))
                {
                    group.Files.Remove(file);
                    moved++;
                }
                else
                {
                    failed++;
                }
            }

            if (group.Files.Count <= 1)
            {
                DuplicateGroups.Remove(group);
            }
        }

        ClosePreview();
        StatusMessage = $"Move oldest: {moved} files moved to {MoveTargetPath}" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void BulkMoveNewest()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0 || string.IsNullOrWhiteSpace(MoveTargetPath))
        {
            return;
        }

        if (!EnsureMoveTargetDirectory())
        {
            return;
        }

        var totalFiles = groups.Sum(g => g.Count - 1);
        var result = System.Windows.MessageBox.Show(
            $"Move newer duplicates from {groups.Count} groups to:\n{MoveTargetPath}\n" +
            $"Will keep the oldest file in each group.\n{totalFiles} files will be moved.",
            "Confirm Move Newest",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var moved = 0;
        var failed = 0;

        foreach (var group in groups)
        {
            var oldest = group.Files.OrderBy(f => f.LastModified).First();
            foreach (var file in group.Files.Where(f => f != oldest).ToList())
            {
                if (MoveFileToTarget(file.FilePath))
                {
                    group.Files.Remove(file);
                    moved++;
                }
                else
                {
                    failed++;
                }
            }

            if (group.Files.Count <= 1)
            {
                DuplicateGroups.Remove(group);
            }
        }

        ClosePreview();
        StatusMessage = $"Move newest: {moved} files moved to {MoveTargetPath}" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void BulkMoveByFilename()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0 || string.IsNullOrWhiteSpace(MoveTargetPath) || string.IsNullOrWhiteSpace(MoveFilterText))
        {
            StatusMessage = "Enter filter text and move target path first.";
            return;
        }

        if (!EnsureMoveTargetDirectory())
        {
            return;
        }

        var filter = MoveFilterText.Trim();
        var matchingFiles = groups
            .SelectMany(g => g.Files.Select(f => new { Group = g, File = f }))
            .Where(x => x.File.FileName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingFiles.Count == 0)
        {
            StatusMessage = $"No files with filename containing \"{filter}\" in selected groups.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Move {matchingFiles.Count} files with filename containing \"{filter}\" to:\n{MoveTargetPath}",
            "Confirm Move by Filename",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var moved = 0;
        var failed = 0;

        foreach (var match in matchingFiles)
        {
            if (MoveFileToTarget(match.File.FilePath))
            {
                match.Group.Files.Remove(match.File);
                moved++;
            }
            else
            {
                failed++;
            }
        }

        // Remove empty groups
        foreach (var group in groups.Where(g => g.Files.Count <= 1).ToList())
        {
            DuplicateGroups.Remove(group);
        }

        ClosePreview();
        StatusMessage = $"Move by filename: {moved} files moved to {MoveTargetPath}" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void BulkMoveByPath()
    {
        var groups = SelectedGroups.ToList();
        if (groups.Count == 0 || string.IsNullOrWhiteSpace(MoveTargetPath) || string.IsNullOrWhiteSpace(MoveFilterText))
        {
            StatusMessage = "Enter filter text and move target path first.";
            return;
        }

        if (!EnsureMoveTargetDirectory())
        {
            return;
        }

        var filter = MoveFilterText.Trim();
        var matchingFiles = groups
            .SelectMany(g => g.Files.Select(f => new { Group = g, File = f }))
            .Where(x => x.File.FilePath.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingFiles.Count == 0)
        {
            StatusMessage = $"No files with path containing \"{filter}\" in selected groups.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Move {matchingFiles.Count} files with path containing \"{filter}\" to:\n{MoveTargetPath}",
            "Confirm Move by Path",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var moved = 0;
        var failed = 0;

        foreach (var match in matchingFiles)
        {
            if (MoveFileToTarget(match.File.FilePath))
            {
                match.Group.Files.Remove(match.File);
                moved++;
            }
            else
            {
                failed++;
            }
        }

        // Remove empty groups
        foreach (var group in groups.Where(g => g.Files.Count <= 1).ToList())
        {
            DuplicateGroups.Remove(group);
        }

        ClosePreview();
        StatusMessage = $"Move by path: {moved} files moved to {MoveTargetPath}" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private bool EnsureMoveTargetDirectory()
    {
        if (Directory.Exists(MoveTargetPath))
        {
            return true;
        }

        try
        {
            Directory.CreateDirectory(MoveTargetPath);
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot create target folder: {ex.Message}";
            return false;
        }
    }

    private bool MoveFileToTarget(string sourcePath)
    {
        try
        {
            var destPath = Path.Combine(MoveTargetPath, Path.GetFileName(sourcePath));

            if (File.Exists(destPath))
            {
                var name = Path.GetFileNameWithoutExtension(sourcePath);
                var ext = Path.GetExtension(sourcePath);
                destPath = Path.Combine(MoveTargetPath, $"{name}_{DateTime.Now:HHmmss}{ext}");
            }

            File.Move(sourcePath, destPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Refreshes the selected group count from IsSelected flags.
    /// </summary>
    public void RefreshSelectedCount()
    {
        SelectedGroupCount = DuplicateGroups.Count(g => g.IsSelected);
    }

    /// <summary>
    /// Sets IsSelected on all visible groups.
    /// </summary>
    public void SelectAllGroups(bool selected)
    {
        foreach (DuplicateGroup group in FilteredDuplicateGroups)
        {
            group.IsSelected = selected;
        }

        RefreshSelectedCount();
    }

    private void UpdateResourceInfo()
    {
        try
        {
            _currentProcess.Refresh();

            // Memory
            var memMb = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
            ResourceMemory = memMb < 1024
                ? $"RAM: {memMb:F0} MB"
                : $"RAM: {memMb / 1024.0:F1} GB";

            // CPU (calculate since last check)
            var now = DateTime.UtcNow;
            var cpuTime = _currentProcess.TotalProcessorTime;
            var elapsed = (now - _lastCheckTime).TotalMilliseconds;

            if (elapsed > 0)
            {
                var cpuUsed = (cpuTime - _lastCpuTime).TotalMilliseconds;
                var cpuPercent = cpuUsed / (Environment.ProcessorCount * elapsed) * 100.0;
                ResourceCpu = $"CPU: {cpuPercent:F1}%";
            }

            _lastCpuTime = cpuTime;
            _lastCheckTime = now;

            // Threads
            ResourceThreads = $"Threads: {_currentProcess.Threads.Count}";
        }
        catch
        {
            // Process may have been disposed
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
