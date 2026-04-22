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
using VbFileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using VbRecycleOption = Microsoft.VisualBasic.FileIO.RecycleOption;
using VbUIOption = Microsoft.VisualBasic.FileIO.UIOption;

namespace WindowsFileManager.ViewModels;

/// <summary>
/// ViewModel for the main duplicate file scanner window.
/// </summary>
[ExcludeFromCodeCoverage]
public class MainViewModel : ViewModelBase
{
    private const int MaxHistoryEntries = 30;

    private readonly DuplicateScannerService _scannerService;
    private readonly IFileSystemService _fileSystem;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _resourceTimer;
    private readonly Process _currentProcess;
    private AppSettings _settings = new();
    private bool _isSwitchingProfile;
    private string _activeProfileName = "Default";
    private string _profileOperationStatus = string.Empty;
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
    private bool _isAnalyticsVisible = true;
    private bool _isFolderControlActive;
    private bool _isHistoryActive;
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
    private string _moveTargetPath = string.Empty;
    private int _selectedFileCount;
    private bool _isActionsVisible;

    // Dynamic filter rule builder state
    private string _rulePatternText = string.Empty;
    private bool _ruleIsRegex;
    private bool _ruleIgnoreCase = true;
    private FilterAction _ruleAction = FilterAction.Include;
    private FilterTarget _ruleTarget = FilterTarget.Filename;

    // Window state
    private double? _windowLeft;
    private double? _windowTop;
    private double? _windowWidth;
    private double? _windowHeight;
    private bool _isMaximized;

    // Exclude folders
    private string _newExcludeFolderName = string.Empty;

    // Folder control
    private string _newFolderSearchPattern = string.Empty;
    private FolderMatchType _newFolderSearchMatchType = FolderMatchType.Match;
    private bool _isFolderSearching;
    private bool _isScanningFolders;
    private string _folderSearchStatus = string.Empty;
    private string _folderPatternAddStatus = string.Empty;
    private int _folderSearchCount;
    private int _selectedFolderCount;
    private bool _areAllFoldersSelected;
    private string _subfolderFilter = string.Empty;
    private string _fileTypeFilter = string.Empty;
    private string _clearSubfolderStatus = string.Empty;
    private bool _folderSearchIncludeSubdirectories = true;
    private bool _flattenRemoveEmptyFolders = true;
    private string _flattenFileTypeFilter = string.Empty;
    private bool _isScanningFlattenTypes;
    private TimeSpan _lastCpuTime;
    private DateTime _lastCheckTime;
    private DuplicateGroup? _selectedDuplicateGroup;
    private string _selectedSortOption = "Size (largest)";
    private int _minDuplicateCount = 2;
    private System.Threading.CancellationTokenSource? _folderSearchCts;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
        : this(CreateDefaultScanner(), CreateDefaultSettings(), new FileSystemService())
    {
    }

    internal MainViewModel(DuplicateScannerService scannerService, SettingsService settingsService, IFileSystemService fileSystem)
    {
        _scannerService = scannerService;
        _settingsService = settingsService;
        _fileSystem = fileSystem;

        ScanCommand = new RelayCommand(_ => ScanAsync(), _ => CanScan());
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsScanning);
        AddFolderCommand = new RelayCommand(_ => AddFolder(), _ => !IsScanning);
        AddFolderByPathCommand = new RelayCommand(_ => AddFolderByPath(), _ => !IsScanning && !string.IsNullOrWhiteSpace(NewFolderPath));
        RemoveFolderCommand = new RelayCommand(p => RemoveFolder(p), _ => !IsScanning);
        SelectAllTargetsCommand = new RelayCommand(_ => SetAllToggles(TargetPaths, true), _ => TargetPaths.Count > 0);
        ClearAllTargetsCommand = new RelayCommand(_ => SetAllToggles(TargetPaths, false), _ => TargetPaths.Count > 0);
        SelectAllExcludesCommand = new RelayCommand(_ => SetAllToggles(ExcludeFolderNames, true), _ => ExcludeFolderNames.Count > 0);
        ClearAllExcludesCommand = new RelayCommand(_ => SetAllToggles(ExcludeFolderNames, false), _ => ExcludeFolderNames.Count > 0);
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

        SelectAllFilesCommand = new RelayCommand(_ => SelectAllFiles(), _ => DuplicateGroups.Count > 0);
        SelectNewerFilesCommand = new RelayCommand(_ => SelectNewerFiles(), _ => DuplicateGroups.Count > 0);
        SelectOlderFilesCommand = new RelayCommand(_ => SelectOlderFiles(), _ => DuplicateGroups.Count > 0);
        ClearFileSelectionCommand = new RelayCommand(_ => ClearFileSelection());

        // Dynamic filter rules
        AddFilterRuleCommand = new RelayCommand(_ => AddFilterRule(), _ => !string.IsNullOrWhiteSpace(RulePatternText));
        RemoveFilterRuleCommand = new RelayCommand(p => RemoveFilterRule(p as FilterRule));
        ClearAllRulesCommand = new RelayCommand(_ => ClearAllRules(), _ => FilterRules.Count > 0);
        EnableAllRulesCommand = new RelayCommand(_ => SetAllRulesEnabled(true), _ => FilterRules.Count > 0);
        DisableAllRulesCommand = new RelayCommand(_ => SetAllRulesEnabled(false), _ => FilterRules.Count > 0);
        ApplyFilterRulesCommand = new RelayCommand(_ => ApplyFilterRules(), _ => DuplicateGroups.Count > 0);
        MoveFilterRuleUpCommand = new RelayCommand(p => MoveFilterRuleUp(p as FilterRule));
        MoveFilterRuleDownCommand = new RelayCommand(p => MoveFilterRuleDown(p as FilterRule));

        // Exclude folders
        AddExcludeFolderCommand = new RelayCommand(_ => AddExcludeFolder(), _ => !string.IsNullOrWhiteSpace(NewExcludeFolderName));
        RemoveExcludeFolderCommand = new RelayCommand(p => RemoveExcludeFolder(p));

        // Folder control
        AddFolderSearchPatternCommand = new RelayCommand(p => AddFolderSearchPattern(p as string));
        RemoveFolderSearchPatternCommand = new RelayCommand(p => RemoveFolderSearchPattern(p));
        MoveSearchPatternUpCommand = new RelayCommand(p => MoveSearchPatternUp(p as FolderSearchPattern));
        MoveSearchPatternDownCommand = new RelayCommand(p => MoveSearchPatternDown(p as FolderSearchPattern));
        SearchFoldersCommand = new RelayCommand(_ => SearchFoldersAsync(), _ => !IsFolderSearching && TargetPaths.Any(t => t.IsEnabled));
        StopFolderSearchCommand = new RelayCommand(_ => StopFolderSearch(), _ => IsFolderSearching);
        ClearFolderSearchCommand = new RelayCommand(_ => ClearFolderSearch(), _ => FolderSearchResults.Count > 0 || DiscoveredSubfolders.Count > 0 || DiscoveredFileTypes.Count > 0);
        UndoLastActionCommand = new RelayCommand(_ => UndoLastAction(), _ => ActionHistory.Count > 0);
        UndoSpecificActionCommand = new RelayCommand(p => UndoSpecificAction(p as ActionHistoryEntry));
        ClearHistoryCommand = new RelayCommand(_ => ClearHistory(), _ => ActionHistory.Count > 0);
        ActionHistory.CollectionChanged += (_, _) => RaiseHistoryAnalytics();
        OpenFolderLocationCommand = new RelayCommand(p => OpenFolderLocation(p as string));

        // Folder actions
        SelectAllFoldersCommand = new RelayCommand(_ => SelectAllFolders(), _ => FolderSearchResults.Count > 0);
        ClearFolderSelectionCommand = new RelayCommand(_ => ClearFolderSelection(), _ => SelectedFolderCount > 0);
        ScanSubfoldersCommand = new RelayCommand(_ => ScanSubfolders(), _ => SelectedFolderCount > 0 && !IsScanningFolders);
        FlattenSelectedFoldersCommand = new RelayCommand(_ => FlattenSelectedFolders(), _ => SelectedFolderCount > 0);
        ScanFlattenFileTypesCommand = new RelayCommand(_ => ScanFlattenFileTypes(), _ => SelectedFolderCount > 0 && !IsScanningFlattenTypes);
        SelectAllFlattenFileTypesCommand = new RelayCommand(_ => SelectAllFlattenFileTypes());
        ClearFlattenFileTypeSelectionCommand = new RelayCommand(_ => ClearFlattenFileTypeSelection());
        ClearSelectedSubfoldersCommand = new RelayCommand(_ => ClearSelectedSubfolders(), _ => DiscoveredSubfolders.Any(s => s.IsSelected));
        SelectAllSubfoldersCommand = new RelayCommand(_ => SelectAllSubfolders());
        ClearSubfolderSelectionCommand = new RelayCommand(_ => ClearSubfolderSelection());
        ClearSelectedFileTypesCommand = new RelayCommand(_ => ClearSelectedFileTypes(), _ => DiscoveredFileTypes.Any(t => t.IsSelected));
        SelectAllFileTypesCommand = new RelayCommand(_ => SelectAllFileTypes());
        ClearFileTypeSelectionCommand = new RelayCommand(_ => ClearFileTypeSelection());

        DeleteSelectedFilesCommand = new RelayCommand(_ => DeleteSelectedFiles(), _ => SelectedFileCount > 0);
        MoveSelectedFilesCommand = new RelayCommand(_ => MoveSelectedFiles(), _ => SelectedFileCount > 0);
        BrowseMoveTargetCommand = new RelayCommand(_ => BrowseMoveTarget());
        SelectAllInGroupCommand = new RelayCommand(p => SelectAllInGroup(p as DuplicateGroup));
        ClearSelectionInGroupCommand = new RelayCommand(p => ClearSelectionInGroup(p as DuplicateGroup));

        // Profile management
        CreateProfileCommand = new RelayCommand(p => CreateProfile(p as string));
        SwitchProfileCommand = new RelayCommand(p => SwitchProfile(p as string), p => p is string name && !string.Equals(name, ActiveProfileName, StringComparison.OrdinalIgnoreCase));
        RenameProfileCommand = new RelayCommand(p => RenameActiveProfile(p as string));
        DeleteProfileCommand = new RelayCommand(_ => DeleteActiveProfile(), _ => ProfileNames.Count > 1);

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
    public ObservableCollection<ToggleItem> TargetPaths { get; } = new();

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

    /// <summary>Gets the enable-all target paths command.</summary>
    public ICommand SelectAllTargetsCommand { get; }

    /// <summary>Gets the disable-all target paths command.</summary>
    public ICommand ClearAllTargetsCommand { get; }

    /// <summary>Gets the enable-all exclude folders command.</summary>
    public ICommand SelectAllExcludesCommand { get; }

    /// <summary>Gets the disable-all exclude folders command.</summary>
    public ICommand ClearAllExcludesCommand { get; }

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
        set
        {
            if (SetProperty(ref _isAutoPreview, value))
            {
                IsPreviewVisible = value;
            }
        }
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
    /// Gets or sets the move target path.
    /// </summary>
    public string MoveTargetPath
    {
        get => _moveTargetPath;
        set => SetProperty(ref _moveTargetPath, value);
    }

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

    // -- Selection commands --

    /// <summary>Gets the select all files command.</summary>
    public ICommand SelectAllFilesCommand { get; }

    /// <summary>Gets the select newer duplicates command.</summary>
    public ICommand SelectNewerFilesCommand { get; }

    /// <summary>Gets the select older duplicates command.</summary>
    public ICommand SelectOlderFilesCommand { get; }

    /// <summary>Gets the clear file selection command.</summary>
    public ICommand ClearFileSelectionCommand { get; }

    // -- Dynamic filter rule commands --

    /// <summary>Gets the add filter rule command.</summary>
    public ICommand AddFilterRuleCommand { get; }

    /// <summary>Gets the remove filter rule command.</summary>
    public ICommand RemoveFilterRuleCommand { get; }

    /// <summary>Gets the clear all rules command.</summary>
    public ICommand ClearAllRulesCommand { get; }

    /// <summary>Gets the enable all rules command.</summary>
    public ICommand EnableAllRulesCommand { get; }

    /// <summary>Gets the disable all rules command.</summary>
    public ICommand DisableAllRulesCommand { get; }

    /// <summary>Gets the apply filter rules command.</summary>
    public ICommand ApplyFilterRulesCommand { get; }

    /// <summary>Gets the move filter rule up (higher priority) command.</summary>
    public ICommand MoveFilterRuleUpCommand { get; }

    /// <summary>Gets the move filter rule down (lower priority) command.</summary>
    public ICommand MoveFilterRuleDownCommand { get; }

    /// <summary>Gets the dynamic filter rules collection.</summary>
    public ObservableCollection<FilterRule> FilterRules { get; } = new();

    /// <summary>Gets the available filter actions for the rule builder.</summary>
    public List<FilterAction> FilterActions { get; } = new() { FilterAction.Include, FilterAction.Exclude };

    /// <summary>Gets the available filter targets for the rule builder.</summary>
    public List<FilterTarget> FilterTargets { get; } = new() { FilterTarget.Filename, FilterTarget.Filepath };

    /// <summary>Gets or sets the rule builder pattern text.</summary>
    public string RulePatternText
    {
        get => _rulePatternText;
        set => SetProperty(ref _rulePatternText, value);
    }

    /// <summary>Gets or sets a value indicating whether the rule builder uses regex.</summary>
    public bool RuleIsRegex
    {
        get => _ruleIsRegex;
        set => SetProperty(ref _ruleIsRegex, value);
    }

    /// <summary>Gets or sets a value indicating whether the rule builder ignores case.</summary>
    public bool RuleIgnoreCase
    {
        get => _ruleIgnoreCase;
        set => SetProperty(ref _ruleIgnoreCase, value);
    }

    /// <summary>Gets or sets the rule builder action (Select/Ignore).</summary>
    public FilterAction RuleAction
    {
        get => _ruleAction;
        set => SetProperty(ref _ruleAction, value);
    }

    /// <summary>Gets or sets the rule builder target (Filename/Filepath).</summary>
    public FilterTarget RuleTarget
    {
        get => _ruleTarget;
        set => SetProperty(ref _ruleTarget, value);
    }

    // -- Exclude folders --

    /// <summary>Gets the exclude folder names collection.</summary>
    public ObservableCollection<ToggleItem> ExcludeFolderNames { get; } = new();

    /// <summary>Gets or sets the new exclude folder name input.</summary>
    public string NewExcludeFolderName
    {
        get => _newExcludeFolderName;
        set => SetProperty(ref _newExcludeFolderName, value);
    }

    /// <summary>Gets the add exclude folder command.</summary>
    public ICommand AddExcludeFolderCommand { get; }

    /// <summary>Gets the remove exclude folder command.</summary>
    public ICommand RemoveExcludeFolderCommand { get; }

    // -- Folder Control --

    /// <summary>Gets the folder search patterns collection.</summary>
    public ObservableCollection<FolderSearchPattern> FolderSearchPatterns { get; } = new();

    /// <summary>Gets the available match types for folder search.</summary>
    public List<FolderMatchType> FolderMatchTypes { get; } = new() { FolderMatchType.Include, FolderMatchType.Match, FolderMatchType.Contains, FolderMatchType.Exclude, FolderMatchType.Mismatch };

    /// <summary>Gets the folder search results collection.</summary>
    public ObservableCollection<FolderSearchResult> FolderSearchResults { get; } = new();

    /// <summary>Gets or sets the new folder search pattern input.</summary>
    public string NewFolderSearchPattern
    {
        get => _newFolderSearchPattern;
        set
        {
            if (SetProperty(ref _newFolderSearchPattern, value))
            {
                FolderPatternAddStatus = string.Empty;
            }
        }
    }

    /// <summary>Gets or sets the match type for the next pattern to add.</summary>
    public FolderMatchType NewFolderSearchMatchType
    {
        get => _newFolderSearchMatchType;
        set => SetProperty(ref _newFolderSearchMatchType, value);
    }

    /// <summary>Gets or sets transient feedback for the Add action (e.g., duplicate message).</summary>
    public string FolderPatternAddStatus
    {
        get => _folderPatternAddStatus;
        set => SetProperty(ref _folderPatternAddStatus, value);
    }

    /// <summary>Gets or sets a value indicating whether folder search is running.</summary>
    public bool IsFolderSearching
    {
        get => _isFolderSearching;
        set => SetProperty(ref _isFolderSearching, value);
    }

    /// <summary>Gets or sets a value indicating whether Scan Folders is running.</summary>
    public bool IsScanningFolders
    {
        get => _isScanningFolders;
        set => SetProperty(ref _isScanningFolders, value);
    }

    /// <summary>Gets or sets the folder search status message.</summary>
    public string FolderSearchStatus
    {
        get => _folderSearchStatus;
        set => SetProperty(ref _folderSearchStatus, value);
    }

    /// <summary>Gets or sets the folder search result count.</summary>
    public int FolderSearchCount
    {
        get => _folderSearchCount;
        set => SetProperty(ref _folderSearchCount, value);
    }

    /// <summary>Gets or sets the number of selected folder results.</summary>
    public int SelectedFolderCount
    {
        get => _selectedFolderCount;
        set => SetProperty(ref _selectedFolderCount, value);
    }

    /// <summary>Gets or sets a value indicating whether all folder results are selected (header checkbox).</summary>
    public bool AreAllFoldersSelected
    {
        get => _areAllFoldersSelected;
        set
        {
            if (SetProperty(ref _areAllFoldersSelected, value))
            {
                foreach (var result in FolderSearchResults)
                {
                    result.IsSelected = value;
                }
            }
        }
    }

    /// <summary>Gets the discovered subfolders from scanning selected results.</summary>
    public ObservableCollection<SubfolderItem> DiscoveredSubfolders { get; } = new();

    /// <summary>Gets or sets the subfolder filter text.</summary>
    public string SubfolderFilter
    {
        get => _subfolderFilter;
        set
        {
            if (SetProperty(ref _subfolderFilter, value))
            {
                OnPropertyChanged(nameof(FilteredSubfolders));
            }
        }
    }

    /// <summary>Gets the filtered subfolders based on search text.</summary>
    public IEnumerable<SubfolderItem> FilteredSubfolders =>
        string.IsNullOrWhiteSpace(SubfolderFilter)
            ? DiscoveredSubfolders
            : DiscoveredSubfolders.Where(s => s.Name.Contains(SubfolderFilter, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets the discovered file types (by extension) from scanning selected results.</summary>
    public ObservableCollection<SubfolderItem> DiscoveredFileTypes { get; } = new();

    /// <summary>Gets or sets the file type filter text.</summary>
    public string FileTypeFilter
    {
        get => _fileTypeFilter;
        set
        {
            if (SetProperty(ref _fileTypeFilter, value))
            {
                OnPropertyChanged(nameof(FilteredFileTypes));
            }
        }
    }

    /// <summary>Gets the filtered file types based on search text.</summary>
    public IEnumerable<SubfolderItem> FilteredFileTypes =>
        string.IsNullOrWhiteSpace(FileTypeFilter)
            ? DiscoveredFileTypes
            : DiscoveredFileTypes.Where(s => s.Name.Contains(FileTypeFilter, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets or sets the clear subfolder status message.</summary>
    public string ClearSubfolderStatus
    {
        get => _clearSubfolderStatus;
        set => SetProperty(ref _clearSubfolderStatus, value);
    }

    /// <summary>Gets or sets a value indicating whether folder search recurses into subdirectories.</summary>
    public bool FolderSearchIncludeSubdirectories
    {
        get => _folderSearchIncludeSubdirectories;
        set => SetProperty(ref _folderSearchIncludeSubdirectories, value);
    }

    /// <summary>Gets or sets a value indicating whether gets or sets whether flatten removes empty subfolders after moving files.</summary>
    public bool FlattenRemoveEmptyFolders
    {
        get => _flattenRemoveEmptyFolders;
        set => SetProperty(ref _flattenRemoveEmptyFolders, value);
    }

    /// <summary>Gets the file types discovered inside subfolders only (root-level files excluded) — for Flatten filtering.</summary>
    public ObservableCollection<SubfolderItem> DiscoveredFlattenFileTypes { get; } = new();

    /// <summary>Gets or sets the filter text for the flatten file-types list.</summary>
    public string FlattenFileTypeFilter
    {
        get => _flattenFileTypeFilter;
        set
        {
            if (SetProperty(ref _flattenFileTypeFilter, value))
            {
                OnPropertyChanged(nameof(FilteredFlattenFileTypes));
            }
        }
    }

    /// <summary>Gets the filtered flatten file types based on search text.</summary>
    public IEnumerable<SubfolderItem> FilteredFlattenFileTypes =>
        string.IsNullOrWhiteSpace(FlattenFileTypeFilter)
            ? DiscoveredFlattenFileTypes
            : DiscoveredFlattenFileTypes.Where(t => t.Name.Contains(FlattenFileTypeFilter, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets or sets a value indicating whether the flatten file-type scan is running.</summary>
    public bool IsScanningFlattenTypes
    {
        get => _isScanningFlattenTypes;
        set => SetProperty(ref _isScanningFlattenTypes, value);
    }

    /// <summary>Gets the add folder search pattern command.</summary>
    public ICommand AddFolderSearchPatternCommand { get; }

    /// <summary>Gets the remove folder search pattern command.</summary>
    public ICommand RemoveFolderSearchPatternCommand { get; }

    /// <summary>Gets the move search pattern up command.</summary>
    public ICommand MoveSearchPatternUpCommand { get; }

    /// <summary>Gets the move search pattern down command.</summary>
    public ICommand MoveSearchPatternDownCommand { get; }

    /// <summary>Gets the search folders command.</summary>
    public ICommand SearchFoldersCommand { get; }

    /// <summary>Gets the stop folder search command.</summary>
    public ICommand StopFolderSearchCommand { get; }

    /// <summary>Gets the clear folder search results command.</summary>
    public ICommand ClearFolderSearchCommand { get; }

    /// <summary>Gets the undo last action command.</summary>
    public ICommand UndoLastActionCommand { get; }

    /// <summary>Gets the undo specific action command (expects ActionHistoryEntry parameter).</summary>
    public ICommand UndoSpecificActionCommand { get; }

    /// <summary>Gets the clear-history command (removes all entries; does NOT undo them).</summary>
    public ICommand ClearHistoryCommand { get; }

    /// <summary>Gets the reversible action history (most recent first).</summary>
    public ObservableCollection<ActionHistoryEntry> ActionHistory { get; } = new();

    /// <summary>Gets the tooltip summary for the Undo button, or empty if nothing to undo.</summary>
    public string UndoTooltip => ActionHistory.Count == 0
        ? "Nothing to undo"
        : $"Undo: {ActionHistory[0].Summary}";

    /// <summary>Gets the total number of history entries.</summary>
    public int HistoryTotalEntries => ActionHistory.Count;

    /// <summary>Gets the total number of move operations in history.</summary>
    public int HistoryMoveOperationCount => ActionHistory.Count(e => e.Kind == ActionHistoryKind.MoveFiles);

    /// <summary>Gets the total number of files moved across all move operations.</summary>
    public int HistoryMoveItemCount => ActionHistory.Where(e => e.Kind == ActionHistoryKind.MoveFiles).Sum(e => e.Moves.Count);

    /// <summary>Gets the total number of recycle-file operations.</summary>
    public int HistoryRecycleFileOperationCount => ActionHistory.Count(e => e.Kind == ActionHistoryKind.RecycleFiles);

    /// <summary>Gets the total number of files sent to Recycle Bin across all such operations.</summary>
    public int HistoryRecycleFileItemCount => ActionHistory.Where(e => e.Kind == ActionHistoryKind.RecycleFiles).Sum(e => e.RecycledPaths.Count);

    /// <summary>Gets the total number of recycle-directory operations.</summary>
    public int HistoryRecycleDirOperationCount => ActionHistory.Count(e => e.Kind == ActionHistoryKind.RecycleDirectories);

    /// <summary>Gets the total number of directories sent to Recycle Bin across all such operations.</summary>
    public int HistoryRecycleDirItemCount => ActionHistory.Where(e => e.Kind == ActionHistoryKind.RecycleDirectories).Sum(e => e.RecycledPaths.Count);

    /// <summary>Gets the open folder location command.</summary>
    public ICommand OpenFolderLocationCommand { get; }

    // -- Folder action commands --

    /// <summary>Gets the select all folder results command.</summary>
    public ICommand SelectAllFoldersCommand { get; }

    /// <summary>Gets the clear folder selection command.</summary>
    public ICommand ClearFolderSelectionCommand { get; }

    /// <summary>Gets the scan subfolders command.</summary>
    public ICommand ScanSubfoldersCommand { get; }

    /// <summary>Gets the flatten (move all files to root) command.</summary>
    public ICommand FlattenSelectedFoldersCommand { get; }

    /// <summary>Gets the scan-subfolder-file-types command for Flatten.</summary>
    public ICommand ScanFlattenFileTypesCommand { get; }

    /// <summary>Gets the select-all-flatten-file-types command.</summary>
    public ICommand SelectAllFlattenFileTypesCommand { get; }

    /// <summary>Gets the clear-flatten-file-type-selection command.</summary>
    public ICommand ClearFlattenFileTypeSelectionCommand { get; }

    /// <summary>Gets the clear selected subfolders command.</summary>
    public ICommand ClearSelectedSubfoldersCommand { get; }

    /// <summary>Gets the select all subfolders command.</summary>
    public ICommand SelectAllSubfoldersCommand { get; }

    /// <summary>Gets the clear subfolder selection command.</summary>
    public ICommand ClearSubfolderSelectionCommand { get; }

    /// <summary>Gets the clear selected file types command.</summary>
    public ICommand ClearSelectedFileTypesCommand { get; }

    /// <summary>Gets the select all file types command.</summary>
    public ICommand SelectAllFileTypesCommand { get; }

    /// <summary>Gets the clear file type selection command.</summary>
    public ICommand ClearFileTypeSelectionCommand { get; }

    // -- Action commands --

    /// <summary>Gets the delete selected files command.</summary>
    public ICommand DeleteSelectedFilesCommand { get; }

    /// <summary>Gets the move selected files command.</summary>
    public ICommand MoveSelectedFilesCommand { get; }

    /// <summary>Gets the browse move target command.</summary>
    public ICommand BrowseMoveTargetCommand { get; }

    /// <summary>Gets the select all files in a single group command.</summary>
    public ICommand SelectAllInGroupCommand { get; }

    /// <summary>
    /// Gets the command to create a new profile. Parameter is the proposed profile name (optional).
    /// </summary>
    public ICommand CreateProfileCommand { get; }

    /// <summary>
    /// Gets the command to switch the active profile. Parameter is the target profile name.
    /// </summary>
    public ICommand SwitchProfileCommand { get; }

    /// <summary>
    /// Gets the command to rename the active profile. Parameter is the new name.
    /// </summary>
    public ICommand RenameProfileCommand { get; }

    /// <summary>
    /// Gets the command to delete the active profile (at least one profile must remain).
    /// </summary>
    public ICommand DeleteProfileCommand { get; }

    /// <summary>
    /// Gets the list of saved profile names, bound to the profile switcher combo box.
    /// </summary>
    public ObservableCollection<string> ProfileNames { get; } = new();

    /// <summary>
    /// Gets or sets the active profile name. Setter via UI drives switching.
    /// </summary>
    public string ActiveProfileName
    {
        get => _activeProfileName;
        set
        {
            if (!string.Equals(_activeProfileName, value, StringComparison.OrdinalIgnoreCase))
            {
                SwitchProfile(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the most recent status message from a profile operation (create/rename/delete/switch).
    /// </summary>
    public string ProfileOperationStatus
    {
        get => _profileOperationStatus;
        set => SetProperty(ref _profileOperationStatus, value);
    }

    /// <summary>Gets the clear file selection in a single group command.</summary>
    public ICommand ClearSelectionInGroupCommand { get; }

    /// <summary>
    /// Gets or sets the count of individually selected files.
    /// </summary>
    public int SelectedFileCount
    {
        get => _selectedFileCount;
        set
        {
            if (SetProperty(ref _selectedFileCount, value))
            {
                OnPropertyChanged(nameof(HasSelectedFiles));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether any files are selected.
    /// </summary>
    public bool HasSelectedFiles => SelectedFileCount > 0;

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
    /// Gets or sets a value indicating whether the analytics panel is visible.
    /// </summary>
    public bool IsAnalyticsVisible
    {
        get => _isAnalyticsVisible;
        set => SetProperty(ref _isAnalyticsVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Folder Control tab is active.
    /// </summary>
    public bool IsFolderControlActive
    {
        get => _isFolderControlActive;
        set => SetProperty(ref _isFolderControlActive, value);
    }

    /// <summary>Gets or sets a value indicating whether the History tab is active.</summary>
    public bool IsHistoryActive
    {
        get => _isHistoryActive;
        set => SetProperty(ref _isHistoryActive, value);
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
    /// Gets the current settings (for window state restoration on load).
    /// </summary>
    /// <returns>The loaded settings.</returns>
    public AppSettings GetSettings() => _settingsService.Load();

    /// <summary>
    /// Stores window position and size to be persisted on next save.
    /// </summary>
    /// <param name="left">Window left position.</param>
    /// <param name="top">Window top position.</param>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    /// <param name="isMaximized">Whether the window is maximized.</param>
    public void SaveWindowState(double left, double top, double width, double height, bool isMaximized)
    {
        _windowLeft = left;
        _windowTop = top;
        _windowWidth = width;
        _windowHeight = height;
        _isMaximized = isMaximized;
    }

    /// <summary>
    /// Saves current settings to disk. Writes live UI state into the active profile, then persists the full <see cref="AppSettings"/>.
    /// </summary>
    public void SaveSettings()
    {
        if (_isSwitchingProfile)
        {
            return;
        }

        var profile = GetOrCreateActiveProfile();
        SnapshotLiveStateInto(profile);
        _settings.ActiveProfileName = ActiveProfileName;
        _settings.ActionHistory = ActionHistory.ToList();
        _settings.WindowLeft = _windowLeft;
        _settings.WindowTop = _windowTop;
        _settings.WindowWidth = _windowWidth;
        _settings.WindowHeight = _windowHeight;
        _settings.IsMaximized = _isMaximized;
        _settingsService.Save(_settings);
    }

    private void LoadSettings()
    {
        _settings = _settingsService.Load();
        _activeProfileName = _settings.ActiveProfileName;
        OnPropertyChanged(nameof(ActiveProfileName));
        RefreshProfileNames();

        var profile = GetOrCreateActiveProfile();
        ApplyProfileToLiveState(profile);

        foreach (var entry in _settings.ActionHistory)
        {
            ActionHistory.Add(entry);
        }
    }

    private ProfileSettings GetOrCreateActiveProfile()
    {
        if (_settings.Profiles.Count == 0)
        {
            var seed = new ProfileSettings { Name = string.IsNullOrWhiteSpace(ActiveProfileName) ? "Default" : ActiveProfileName };
            _settings.Profiles.Add(seed);
            _settings.ActiveProfileName = seed.Name;
            _activeProfileName = seed.Name;
        }

        var profile = _settings.Profiles.FirstOrDefault(p => string.Equals(p.Name, ActiveProfileName, StringComparison.OrdinalIgnoreCase));
        if (profile == null)
        {
            profile = _settings.Profiles[0];
            _activeProfileName = profile.Name;
            _settings.ActiveProfileName = profile.Name;
            OnPropertyChanged(nameof(ActiveProfileName));
        }

        return profile;
    }

    private void SnapshotLiveStateInto(ProfileSettings profile)
    {
        profile.TargetPaths = TargetPaths.Select(t => t.Value).ToList();
        profile.DisabledTargetPaths = TargetPaths.Where(t => !t.IsEnabled).Select(t => t.Value).ToList();
        profile.IncludeSubdirectories = IncludeSubdirectories;
        profile.IsMiniPreview = IsMiniPreview;
        profile.IsAutoPreview = IsAutoPreview;
        profile.IsAutoPlay = IsAutoPlay;
        profile.SelectedSortOption = SelectedSortOption;
        profile.Volume = MediaVolume;
        profile.MoveTargetPath = MoveTargetPath;
        profile.ExcludeFolderNames = ExcludeFolderNames.Select(t => t.Value).ToList();
        profile.DisabledExcludeFolderNames = ExcludeFolderNames.Where(t => !t.IsEnabled).Select(t => t.Value).ToList();
        profile.FilterRules = FilterRules.ToList();
        profile.FolderSearchPatterns = FolderSearchPatterns.ToList();
        profile.FolderSearchResultPaths = FolderSearchResults.Select(r => r.FullPath).ToList();
        profile.SelectedFolderSearchResultPaths = FolderSearchResults.Where(r => r.IsSelected).Select(r => r.FullPath).ToList();
    }

    private void ApplyProfileToLiveState(ProfileSettings profile)
    {
        foreach (var t in TargetPaths)
        {
            t.PropertyChanged -= ToggleItem_PropertyChanged;
        }

        foreach (var t in ExcludeFolderNames)
        {
            t.PropertyChanged -= ToggleItem_PropertyChanged;
        }

        foreach (var r in FolderSearchResults)
        {
            r.PropertyChanged -= FolderResult_PropertyChanged;
        }

        TargetPaths.Clear();
        ExcludeFolderNames.Clear();
        FilterRules.Clear();
        FolderSearchPatterns.Clear();
        FolderSearchResults.Clear();
        DiscoveredSubfolders.Clear();
        DiscoveredFileTypes.Clear();
        DiscoveredFlattenFileTypes.Clear();

        IncludeSubdirectories = profile.IncludeSubdirectories;
        IsMiniPreview = profile.IsMiniPreview;
        IsAutoPreview = profile.IsAutoPreview;
        IsPreviewVisible = IsAutoPreview;
        IsAutoPlay = profile.IsAutoPlay;
        SelectedSortOption = profile.SelectedSortOption;
        MediaVolume = profile.Volume;
        MoveTargetPath = profile.MoveTargetPath;

        var disabledTargets = new HashSet<string>(profile.DisabledTargetPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var path in profile.TargetPaths)
        {
            var item = new ToggleItem(path) { IsEnabled = !disabledTargets.Contains(path) };
            item.PropertyChanged += ToggleItem_PropertyChanged;
            TargetPaths.Add(item);
        }

        var disabledExcludes = new HashSet<string>(profile.DisabledExcludeFolderNames, StringComparer.OrdinalIgnoreCase);
        foreach (var name in profile.ExcludeFolderNames)
        {
            var item = new ToggleItem(name) { IsEnabled = !disabledExcludes.Contains(name) };
            item.PropertyChanged += ToggleItem_PropertyChanged;
            ExcludeFolderNames.Add(item);
        }

        foreach (var rule in profile.FilterRules)
        {
            FilterRules.Add(rule);
        }

        foreach (var pattern in profile.FolderSearchPatterns)
        {
            FolderSearchPatterns.Add(pattern);
        }

        var selectedPaths = new HashSet<string>(profile.SelectedFolderSearchResultPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var path in profile.FolderSearchResultPaths)
        {
            if (!_fileSystem.DirectoryExists(path))
            {
                continue;
            }

            var result = new FolderSearchResult
            {
                FullPath = path,
                FolderName = Path.GetFileName(path),
                ParentPath = Path.GetDirectoryName(path) ?? string.Empty,
                IsSelected = selectedPaths.Contains(path),
            };
            result.PropertyChanged += FolderResult_PropertyChanged;
            FolderSearchResults.Add(result);
        }

        FolderSearchCount = FolderSearchResults.Count;
        SelectedFolderCount = FolderSearchResults.Count(r => r.IsSelected);
        _areAllFoldersSelected = FolderSearchResults.Count > 0 && FolderSearchResults.All(r => r.IsSelected);
        OnPropertyChanged(nameof(AreAllFoldersSelected));
        OnPropertyChanged(nameof(FilteredSubfolders));
        OnPropertyChanged(nameof(FilteredFileTypes));

        FolderSearchStatus = FolderSearchCount > 0
            ? $"Restored {FolderSearchCount} folders from profile '{profile.Name}'."
            : string.Empty;
        ClearSubfolderStatus = string.Empty;

        if (FolderSearchCount > 0)
        {
            _ = ComputeRestoredSizesAsync();
        }

        RefreshRulePriorities();
        RefreshSearchPatternPriorities();
    }

    private void RefreshProfileNames()
    {
        ProfileNames.Clear();
        foreach (var p in _settings.Profiles)
        {
            ProfileNames.Add(p.Name);
        }
    }

    private void CreateProfile(string? suggestedName)
    {
        var baseName = string.IsNullOrWhiteSpace(suggestedName) ? "New Profile" : suggestedName!.Trim();
        var name = baseName;
        var i = 2;
        while (_settings.Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{baseName} ({i++})";
        }

        var source = GetOrCreateActiveProfile();
        SnapshotLiveStateInto(source);

        var profile = CloneProfile(source, name);
        _settings.Profiles.Add(profile);
        RefreshProfileNames();
        ProfileOperationStatus = $"Created profile '{name}' (copied from '{source.Name}').";
        SwitchProfile(name);
    }

    private static ProfileSettings CloneProfile(ProfileSettings source, string newName) => new()
    {
        Name = newName,
        TargetPaths = new List<string>(source.TargetPaths),
        DisabledTargetPaths = new List<string>(source.DisabledTargetPaths),
        IncludeSubdirectories = source.IncludeSubdirectories,
        MinimumFileSize = source.MinimumFileSize,
        IsMiniPreview = source.IsMiniPreview,
        IsAutoPreview = source.IsAutoPreview,
        IsAutoPlay = source.IsAutoPlay,
        SelectedSortOption = source.SelectedSortOption,
        Volume = source.Volume,
        MoveTargetPath = source.MoveTargetPath,
        ExcludeFolderNames = new List<string>(source.ExcludeFolderNames),
        DisabledExcludeFolderNames = new List<string>(source.DisabledExcludeFolderNames),
        FilterRules = source.FilterRules.Select(r => new FilterRule
        {
            Pattern = r.Pattern,
            IsRegex = r.IsRegex,
            IgnoreCase = r.IgnoreCase,
            Action = r.Action,
            Target = r.Target,
            IsEnabled = r.IsEnabled,
            Priority = r.Priority,
        }).ToList(),
        FolderSearchPatterns = source.FolderSearchPatterns.Select(p => new FolderSearchPattern
        {
            Pattern = p.Pattern,
            MatchType = p.MatchType,
            IsEnabled = p.IsEnabled,
            Priority = p.Priority,
        }).ToList(),
        FolderSearchResultPaths = new List<string>(source.FolderSearchResultPaths),
        SelectedFolderSearchResultPaths = new List<string>(source.SelectedFolderSearchResultPaths),
    };

    private void SwitchProfile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (string.Equals(name, ActiveProfileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var target = _settings.Profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            return;
        }

        var current = _settings.Profiles.FirstOrDefault(p => string.Equals(p.Name, ActiveProfileName, StringComparison.OrdinalIgnoreCase));
        if (current != null)
        {
            SnapshotLiveStateInto(current);
        }

        _settings.ActiveProfileName = target.Name;
        _activeProfileName = target.Name;
        OnPropertyChanged(nameof(ActiveProfileName));

        _isSwitchingProfile = true;
        try
        {
            ApplyProfileToLiveState(target);
        }
        finally
        {
            _isSwitchingProfile = false;
        }

        _settingsService.Save(_settings);
        ProfileOperationStatus = $"Switched to '{target.Name}'.";
    }

    private void RenameActiveProfile(string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        var trimmed = newName.Trim();
        if (string.Equals(trimmed, ActiveProfileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_settings.Profiles.Any(p => string.Equals(p.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            ProfileOperationStatus = $"A profile named '{trimmed}' already exists.";
            return;
        }

        var profile = GetOrCreateActiveProfile();
        profile.Name = trimmed;
        _activeProfileName = trimmed;
        _settings.ActiveProfileName = trimmed;
        OnPropertyChanged(nameof(ActiveProfileName));
        RefreshProfileNames();
        _settingsService.Save(_settings);
        ProfileOperationStatus = $"Renamed to '{trimmed}'.";
    }

    private void DeleteActiveProfile()
    {
        if (_settings.Profiles.Count <= 1)
        {
            return;
        }

        var current = _settings.Profiles.FirstOrDefault(p => string.Equals(p.Name, ActiveProfileName, StringComparison.OrdinalIgnoreCase));
        if (current == null)
        {
            return;
        }

        _settings.Profiles.Remove(current);
        var next = _settings.Profiles[0];
        _settings.ActiveProfileName = next.Name;
        _activeProfileName = next.Name;
        OnPropertyChanged(nameof(ActiveProfileName));
        RefreshProfileNames();

        _isSwitchingProfile = true;
        try
        {
            ApplyProfileToLiveState(next);
        }
        finally
        {
            _isSwitchingProfile = false;
        }

        _settingsService.Save(_settings);
        ProfileOperationStatus = $"Deleted profile. Now on '{next.Name}'.";
    }

    private bool CanScan() => !IsScanning && TargetPaths.Any(t => t.IsEnabled);

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
                TargetPaths = TargetPaths.Where(t => t.IsEnabled).Select(t => t.Value).ToList(),
                IncludeSubdirectories = IncludeSubdirectories,
                ExcludeFolderNames = ExcludeFolderNames.Where(t => t.IsEnabled).Select(t => t.Value).ToList(),
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

        if (dialog.ShowDialog() == true && !TargetPaths.Any(t => t.Value == dialog.FolderName))
        {
            var item = new ToggleItem(dialog.FolderName);
            item.PropertyChanged += ToggleItem_PropertyChanged;
            TargetPaths.Add(item);
            SaveSettings();
        }
    }

    private void AddFolderByPath()
    {
        var path = NewFolderPath.Trim();
        if (!string.IsNullOrEmpty(path) && !TargetPaths.Any(t => t.Value == path))
        {
            var item = new ToggleItem(path);
            item.PropertyChanged += ToggleItem_PropertyChanged;
            TargetPaths.Add(item);
            NewFolderPath = string.Empty;
            SaveSettings();
        }
    }

    private void RemoveFolder(object? param)
    {
        if (param is ToggleItem item)
        {
            TargetPaths.Remove(item);
            SaveSettings();
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
            $"Send this file to the Recycle Bin?\n\n{file.FilePath}",
            "Confirm Recycle File",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var recycledPath = file.FilePath;
            RecycleFile(recycledPath);
            PushHistory(new ActionHistoryEntry
            {
                Kind = ActionHistoryKind.RecycleFiles,
                RecycledPaths = new List<string> { recycledPath },
                Summary = $"Recycled {Path.GetFileName(recycledPath)}",
            });

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
            $"Send ALL {group.Files.Count} files in this group to the Recycle Bin?\n\n{fileList}",
            "Confirm Recycle All",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = 0;
        var failed = 0;

        var recycled = new List<string>();
        foreach (var file in group.Files.ToList())
        {
            try
            {
                RecycleFile(file.FilePath);
                recycled.Add(file.FilePath);
                deleted++;
            }
            catch
            {
                failed++;
            }
        }

        if (recycled.Count > 0)
        {
            PushHistory(new ActionHistoryEntry
            {
                Kind = ActionHistoryKind.RecycleFiles,
                RecycledPaths = recycled,
                Summary = $"Recycled {recycled.Count} duplicate files",
            });
        }

        DuplicateGroups.Remove(group);
        ClosePreview();

        StatusMessage = failed == 0
            ? $"Sent all {deleted} files in group to Recycle Bin"
            : $"Sent {deleted} files to Recycle Bin, {failed} failed";
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
        IsAutoPreview = false;
        PreviewType = "none";
        PreviewImage = null;
        PreviewMediaUri = null;
        PreviewText = null;
        PreviewFileName = null;
        PreviewFileSize = null;
    }

    // ── SELECTION METHODS ──
    private void SelectAllFiles()
    {
        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
            {
                file.IsFileSelected = true;
            }
        }

        var ignored = ApplyIgnoreRules();
        RefreshSelectedFileCount();
        StatusMessage = $"Selected all {SelectedFileCount} files in {DuplicateGroups.Count} groups." +
            (ignored > 0 ? $" ({ignored} excluded by ignore rules)" : string.Empty);
    }

    private void SelectNewerFiles()
    {
        ClearFileSelection();
        var selected = 0;

        foreach (var group in DuplicateGroups)
        {
            if (group.Files.Count <= 1)
            {
                continue;
            }

            var oldest = group.Files.OrderBy(f => f.LastModified).First();
            foreach (var file in group.Files.Where(f => f != oldest))
            {
                file.IsFileSelected = true;
                selected++;
            }
        }

        var ignored = ApplyIgnoreRules();
        selected -= ignored;
        RefreshSelectedFileCount();
        StatusMessage = $"Selected {selected} newer duplicates (keeping oldest in each group)." +
            (ignored > 0 ? $" ({ignored} excluded by ignore rules)" : string.Empty);
    }

    private void SelectOlderFiles()
    {
        ClearFileSelection();
        var selected = 0;

        foreach (var group in DuplicateGroups)
        {
            if (group.Files.Count <= 1)
            {
                continue;
            }

            var newest = group.Files.OrderByDescending(f => f.LastModified).First();
            foreach (var file in group.Files.Where(f => f != newest))
            {
                file.IsFileSelected = true;
                selected++;
            }
        }

        var ignored = ApplyIgnoreRules();
        selected -= ignored;
        RefreshSelectedFileCount();
        StatusMessage = $"Selected {selected} older duplicates (keeping newest in each group)." +
            (ignored > 0 ? $" ({ignored} excluded by ignore rules)" : string.Empty);
    }

    private int ApplyIgnoreRules()
    {
        var ignoreRules = FilterRules.Where(r => r.Action == FilterAction.Exclude).ToList();
        if (ignoreRules.Count == 0)
        {
            return 0;
        }

        var deselected = 0;
        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
            {
                if (!file.IsFileSelected)
                {
                    continue;
                }

                var matchesAnyIgnore = ignoreRules.Any(rule =>
                {
                    var input = rule.Target == FilterTarget.Filename ? file.FileName : file.FilePath;
                    return MatchesFilter(input, rule.Pattern, rule.IsRegex, rule.IgnoreCase);
                });

                if (matchesAnyIgnore)
                {
                    file.IsFileSelected = false;
                    deselected++;
                }
            }
        }

        return deselected;
    }

    private void AddFilterRule()
    {
        var pattern = RulePatternText.Trim();
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }

        FilterRules.Add(new FilterRule
        {
            Pattern = pattern,
            IsRegex = RuleIsRegex,
            IgnoreCase = RuleIgnoreCase,
            Action = RuleAction,
            Target = RuleTarget,
        });

        RefreshRulePriorities();

        // Clear builder for next input
        RulePatternText = string.Empty;
        RuleIsRegex = false;
        RuleIgnoreCase = true;
        RuleAction = FilterAction.Include;
        RuleTarget = FilterTarget.Filename;

        StatusMessage = $"Added filter rule #{FilterRules.Count}: {FilterRules[^1].DisplaySummary}";
        SaveSettings();
    }

    private void RemoveFilterRule(FilterRule? rule)
    {
        if (rule != null)
        {
            FilterRules.Remove(rule);
            RefreshRulePriorities();
            StatusMessage = "Removed filter rule.";
            SaveSettings();
        }
    }

    private void MoveFilterRuleUp(FilterRule? rule)
    {
        if (rule == null)
        {
            return;
        }

        var index = FilterRules.IndexOf(rule);
        if (index > 0)
        {
            FilterRules.Move(index, index - 1);
            RefreshRulePriorities();
            SaveSettings();
        }
    }

    private void MoveFilterRuleDown(FilterRule? rule)
    {
        if (rule == null)
        {
            return;
        }

        var index = FilterRules.IndexOf(rule);
        if (index >= 0 && index < FilterRules.Count - 1)
        {
            FilterRules.Move(index, index + 1);
            RefreshRulePriorities();
            SaveSettings();
        }
    }

    private void RefreshRulePriorities()
    {
        for (var i = 0; i < FilterRules.Count; i++)
        {
            FilterRules[i].Priority = i + 1;
        }
    }

    private void ApplyFilterRules()
    {
        if (FilterRules.Count == 0)
        {
            StatusMessage = "No filter rules to apply. Add rules first.";
            return;
        }

        ClearFileSelection();

        // Process rules from lowest priority (last) to highest (first).
        // Higher priority rules override lower ones — the last write wins.
        var rulesHighToLow = FilterRules.Reverse().ToList();

        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
            {
                // Find the highest-priority enabled rule that matches this file
                foreach (var rule in FilterRules)
                {
                    if (!rule.IsEnabled)
                    {
                        continue;
                    }

                    var input = rule.Target == FilterTarget.Filename ? file.FileName : file.FilePath;
                    if (MatchesFilter(input, rule.Pattern, rule.IsRegex, rule.IgnoreCase))
                    {
                        file.IsFileSelected = rule.Action == FilterAction.Include;
                        break; // highest priority match wins
                    }
                }
            }
        }

        RefreshSelectedFileCount();
        var selected = SelectedFileCount;
        var enabledCount = FilterRules.Count(r => r.IsEnabled);
        StatusMessage = $"Applied {enabledCount}/{FilterRules.Count} rules (priority order): {selected} files selected.";
    }

    private static bool MatchesFilter(string input, string filter, bool useRegex, bool ignoreCase)
    {
        if (useRegex)
        {
            try
            {
                var options = ignoreCase
                    ? System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    : System.Text.RegularExpressions.RegexOptions.None;
                return System.Text.RegularExpressions.Regex.IsMatch(input, filter, options, TimeSpan.FromSeconds(1));
            }
            catch
            {
                return false;
            }
        }

        var comparison = ignoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return input.Contains(filter, comparison);
    }

    private void ClearFileSelection()
    {
        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
            {
                file.IsFileSelected = false;
            }
        }

        RefreshSelectedFileCount();
    }

    private void ClearAllRules()
    {
        FilterRules.Clear();
        RefreshRulePriorities();
        StatusMessage = "All filter rules cleared.";
        SaveSettings();
    }

    private void SetAllRulesEnabled(bool enabled)
    {
        foreach (var rule in FilterRules)
        {
            rule.IsEnabled = enabled;
        }

        StatusMessage = enabled
            ? $"All {FilterRules.Count} rules enabled."
            : $"All {FilterRules.Count} rules disabled.";
        SaveSettings();
    }

    private void AddExcludeFolder()
    {
        var name = NewExcludeFolderName.Trim();
        if (!string.IsNullOrEmpty(name) && !ExcludeFolderNames.Any(t => t.Value == name))
        {
            var item = new ToggleItem(name);
            item.PropertyChanged += ToggleItem_PropertyChanged;
            ExcludeFolderNames.Add(item);
            NewExcludeFolderName = string.Empty;
            SaveSettings();
        }
    }

    private void RemoveExcludeFolder(object? param)
    {
        if (param is ToggleItem item)
        {
            ExcludeFolderNames.Remove(item);
            SaveSettings();
        }
    }

    // ── FOLDER CONTROL METHODS ──
    private void AddFolderSearchPattern(string? input)
    {
        var pattern = (input ?? NewFolderSearchPattern ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(pattern))
        {
            FolderPatternAddStatus = "Enter a name first";
            return;
        }

        if (FolderSearchPatterns.Any(t => t.Pattern == pattern && t.MatchType == NewFolderSearchMatchType))
        {
            FolderPatternAddStatus = $"'{pattern}' with {NewFolderSearchMatchType} already in the list";
            return;
        }

        FolderSearchPatterns.Add(new FolderSearchPattern { Pattern = pattern, MatchType = NewFolderSearchMatchType });
        NewFolderSearchPattern = string.Empty;
        FolderPatternAddStatus = string.Empty;
        RefreshSearchPatternPriorities();
        SaveSettings();
    }

    private void RemoveFolderSearchPattern(object? param)
    {
        if (param is FolderSearchPattern item)
        {
            FolderSearchPatterns.Remove(item);
            RefreshSearchPatternPriorities();
            SaveSettings();
        }
    }

    private void MoveSearchPatternUp(FolderSearchPattern? pattern)
    {
        if (pattern == null)
        {
            return;
        }

        var index = FolderSearchPatterns.IndexOf(pattern);
        if (index > 0)
        {
            FolderSearchPatterns.Move(index, index - 1);
            RefreshSearchPatternPriorities();
            SaveSettings();
        }
    }

    private void MoveSearchPatternDown(FolderSearchPattern? pattern)
    {
        if (pattern == null)
        {
            return;
        }

        var index = FolderSearchPatterns.IndexOf(pattern);
        if (index >= 0 && index < FolderSearchPatterns.Count - 1)
        {
            FolderSearchPatterns.Move(index, index + 1);
            RefreshSearchPatternPriorities();
            SaveSettings();
        }
    }

    private void RefreshSearchPatternPriorities()
    {
        for (var i = 0; i < FolderSearchPatterns.Count; i++)
        {
            FolderSearchPatterns[i].Priority = i + 1;
        }
    }

    private void ClearFolderSearch()
    {
        foreach (var r in FolderSearchResults)
        {
            r.PropertyChanged -= FolderResult_PropertyChanged;
        }

        FolderSearchResults.Clear();
        DiscoveredSubfolders.Clear();
        DiscoveredFileTypes.Clear();
        OnPropertyChanged(nameof(FilteredSubfolders));
        OnPropertyChanged(nameof(FilteredFileTypes));
        FolderSearchCount = 0;
        SelectedFolderCount = 0;
        AreAllFoldersSelected = false;
        FolderSearchStatus = string.Empty;
        ClearSubfolderStatus = string.Empty;
        SaveSettings();
    }

    private async void SearchFoldersAsync()
    {
        _folderSearchCts?.Cancel();
        _folderSearchCts?.Dispose();
        _folderSearchCts = new System.Threading.CancellationTokenSource();
        var token = _folderSearchCts.Token;

        IsFolderSearching = true;
        foreach (var r in FolderSearchResults)
        {
            r.PropertyChanged -= FolderResult_PropertyChanged;
        }

        FolderSearchResults.Clear();
        FolderSearchCount = 0;
        SelectedFolderCount = 0;
        FolderSearchStatus = "Searching folders...";

        var targetPaths = TargetPaths.Where(t => t.IsEnabled).Select(t => t.Value).ToList();
        var excludeNames = new HashSet<string>(
            ExcludeFolderNames.Where(t => t.IsEnabled).Select(t => t.Value),
            StringComparer.OrdinalIgnoreCase);
        var patterns = FolderSearchPatterns.Where(t => t.IsEnabled).ToList();
        var recurse = true;

        try
        {
            var results = await Task.Run(
                () =>
                {
                    var found = new List<FolderSearchResult>();
                    foreach (var rootPath in targetPaths)
                    {
                        token.ThrowIfCancellationRequested();
                        if (!_fileSystem.DirectoryExists(rootPath))
                        {
                            continue;
                        }

                        SearchFoldersRecursive(rootPath, patterns, excludeNames, found, recurse, token);
                    }

                    foreach (var r in found)
                    {
                        token.ThrowIfCancellationRequested();
                        r.TotalSize = GetDirectorySize(r.FullPath, token);
                    }

                    return found;
                }, token);

            foreach (var result in results)
            {
                result.PropertyChanged += FolderResult_PropertyChanged;
                FolderSearchResults.Add(result);
            }

            FolderSearchCount = results.Count;
            SelectedFolderCount = 0;
            FolderSearchStatus = patterns.Count > 0
                ? $"Found {results.Count} folders matching {patterns.Count} patterns."
                : $"Found {results.Count} folders (no filter).";
        }
        catch (OperationCanceledException)
        {
            FolderSearchStatus = $"Search stopped. {FolderSearchResults.Count} folders found so far.";
        }
        catch (Exception ex)
        {
            FolderSearchStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsFolderSearching = false;
            SaveSettings();
        }
    }

    private void StopFolderSearch()
    {
        _folderSearchCts?.Cancel();
    }

    private void SearchFoldersRecursive(
        string currentPath,
        List<FolderSearchPattern> patterns,
        HashSet<string> excludeNames,
        List<FolderSearchResult> results,
        bool recurse,
        System.Threading.CancellationToken token)
    {
        IEnumerable<string> subDirs;
        try
        {
            subDirs = _fileSystem.EnumerateDirectories(currentPath);
        }
        catch
        {
            return;
        }

        foreach (var subDir in subDirs)
        {
            token.ThrowIfCancellationRequested();
            var dirName = Path.GetFileName(subDir);

            if (excludeNames.Contains(dirName))
            {
                continue;
            }

            // No patterns = include all folders; otherwise filter by patterns
            if (patterns.Count == 0)
            {
                results.Add(new FolderSearchResult
                {
                    FullPath = subDir,
                    FolderName = dirName,
                    ParentPath = currentPath,
                    MatchedPattern = string.Empty,
                });
            }
            else
            {
                // All patterns must pass for the folder to be included
                var allMatch = true;
                var matchedPatternName = string.Empty;
                foreach (var pattern in patterns)
                {
                    bool isMatch = pattern.MatchType switch
                    {
                        FolderMatchType.Include => dirName.Contains(pattern.Pattern, StringComparison.OrdinalIgnoreCase),
                        FolderMatchType.Match => dirName.Equals(pattern.Pattern, StringComparison.OrdinalIgnoreCase),
                        FolderMatchType.Contains => FolderContainsItem(subDir, pattern.Pattern),
                        FolderMatchType.Exclude => !dirName.Contains(pattern.Pattern, StringComparison.OrdinalIgnoreCase),
                        FolderMatchType.Mismatch => !dirName.Equals(pattern.Pattern, StringComparison.OrdinalIgnoreCase),
                        _ => false,
                    };

                    if (!isMatch)
                    {
                        allMatch = false;
                        break;
                    }

                    if (matchedPatternName.Length == 0)
                    {
                        matchedPatternName = pattern.Pattern;
                    }
                }

                if (allMatch)
                {
                    results.Add(new FolderSearchResult
                    {
                        FullPath = subDir,
                        FolderName = dirName,
                        ParentPath = currentPath,
                        MatchedPattern = matchedPatternName,
                    });
                }
            }

            // Recurse deeper if enabled
            if (recurse)
            {
                SearchFoldersRecursive(subDir, patterns, excludeNames, results, recurse, token);
            }
        }
    }

    /// <summary>
    /// Checks if a folder contains a child item matching the pattern.
    /// Supports: exact name (e.g. ".git", "package.json"), wildcard extension (e.g. "*.py", "*.sln").
    /// Checks both subfolders and files in the immediate directory.
    /// </summary>
    private static bool FolderContainsItem(string folderPath, string pattern)
    {
        try
        {
            if (pattern.StartsWith("*."))
            {
                // Wildcard extension match — check files with that extension
                var extension = pattern[1..]; // e.g. ".py"
                foreach (var file in Directory.EnumerateFiles(folderPath))
                {
                    if (Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // Exact name match — check subfolders and files
                var targetPath = Path.Combine(folderPath, pattern);
                if (Directory.Exists(targetPath) || File.Exists(targetPath))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Access denied or other IO error — skip
        }

        return false;
    }

    private static void OpenFolderLocation(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
        }
    }

    private void ToggleItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToggleItem.IsEnabled))
        {
            SaveSettings();
        }
    }

    // ── FOLDER ACTION METHODS ──
    private void FolderResult_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FolderSearchResult.IsSelected))
        {
            SelectedFolderCount = FolderSearchResults.Count(r => r.IsSelected);
            _areAllFoldersSelected = FolderSearchResults.Count > 0 && FolderSearchResults.All(r => r.IsSelected);
            OnPropertyChanged(nameof(AreAllFoldersSelected));
            SaveSettings();
        }
    }

    private void SelectAllFolders()
    {
        foreach (var result in FolderSearchResults)
        {
            result.IsSelected = true;
        }
    }

    private void ClearFolderSelection()
    {
        foreach (var result in FolderSearchResults)
        {
            result.IsSelected = false;
        }
    }

    private async void FlattenSelectedFolders()
    {
        var selected = FolderSearchResults.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var selectedTypes = DiscoveredFlattenFileTypes.Where(t => t.IsSelected).Select(t => t.Name).ToList();
        HashSet<string>? extensionFilter = selectedTypes.Count > 0
            ? new HashSet<string>(selectedTypes, StringComparer.OrdinalIgnoreCase)
            : null;

        var removeEmpty = FlattenRemoveEmptyFolders;
        int totalFiles;
        try
        {
            totalFiles = selected.Sum(f =>
                CountFilesInSubdirectories(f.FullPath, extensionFilter));
        }
        catch (Exception ex)
        {
            ClearSubfolderStatus = $"Error counting files: {ex.Message}";
            return;
        }

        if (totalFiles == 0)
        {
            ClearSubfolderStatus = extensionFilter != null
                ? "No files of selected types found in subdirectories."
                : "No files in subdirectories to move.";
            return;
        }

        var filterDesc = extensionFilter != null
            ? $"\n• Only file types: {string.Join(", ", extensionFilter)}"
            : string.Empty;
        var emptyDesc = removeEmpty ? "\n• Empty subdirectories will be removed." : string.Empty;

        var result = System.Windows.MessageBox.Show(
            $"Move {totalFiles} files from subdirectories up to the root of {selected.Count} selected folder(s)?{filterDesc}\n• Name conflicts resolved with (2), (3), etc.{emptyDesc}\n• Undo is available in the History tab.",
            "Confirm Move Files to Root",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        ClearSubfolderStatus = "Moving files...";
        var moves = new List<ActionHistoryMove>();
        var failed = await System.Threading.Tasks.Task.Run(() =>
        {
            int f = 0;
            foreach (var folder in selected)
            {
                (int mv, int fl) = FlattenFolder(folder.FullPath, moves, extensionFilter, removeEmpty);
                f += fl;
            }

            return f;
        });

        if (moves.Count > 0)
        {
            PushHistory(new ActionHistoryEntry
            {
                Kind = ActionHistoryKind.MoveFiles,
                Moves = moves,
                Summary = $"Moved {moves.Count} files to folder root",
            });
        }

        var removedDesc = removeEmpty ? " Empty subdirectories removed." : string.Empty;
        ClearSubfolderStatus = failed == 0
            ? $"Moved {moves.Count} files to root of {selected.Count} folder(s).{removedDesc}"
            : $"Moved {moves.Count} files. {failed} failed (access denied, in use, or name conflict).";

        if (moves.Count > 0)
        {
            ScanFlattenFileTypes();
        }
    }

    private void SelectAllFlattenFileTypes()
    {
        foreach (var t in FilteredFlattenFileTypes)
        {
            t.IsSelected = true;
        }
    }

    private void ClearFlattenFileTypeSelection()
    {
        foreach (var t in DiscoveredFlattenFileTypes)
        {
            t.IsSelected = false;
        }
    }

    private async void ScanFlattenFileTypes()
    {
        var selected = FolderSearchResults.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        IsScanningFlattenTypes = true;
        DiscoveredFlattenFileTypes.Clear();
        FlattenFileTypeFilter = string.Empty;
        OnPropertyChanged(nameof(FilteredFlattenFileTypes));
        var folderPaths = selected.Select(f => f.FullPath).ToList();

        try
        {
            var items = await System.Threading.Tasks.Task.Run(() =>
            {
                var data = new Dictionary<string, List<SubfolderLocation>>(StringComparer.OrdinalIgnoreCase);
                foreach (var root in folderPaths)
                {
                    CollectSubfolderFileTypes(root, root, data);
                }

                return data
                    .OrderByDescending(k => k.Value.Count)
                    .ThenBy(k => k.Key)
                    .Select(kvp => new SubfolderItem
                    {
                        Name = kvp.Key,
                        Count = kvp.Value.Count,
                        Locations = kvp.Value,
                        TotalSize = kvp.Value.Sum(loc => SafeFileSize(loc.FullPath)),
                    })
                    .ToList();
            }).ConfigureAwait(true);

            foreach (var item in items)
            {
                DiscoveredFlattenFileTypes.Add(item);
            }

            OnPropertyChanged(nameof(FilteredFlattenFileTypes));
        }
        finally
        {
            IsScanningFlattenTypes = false;
        }
    }

    private void CollectSubfolderFileTypes(string path, string rootParent, Dictionary<string, List<SubfolderLocation>> data)
    {
        IEnumerable<string> subDirs;
        try
        {
            subDirs = _fileSystem.EnumerateDirectories(path).ToList();
        }
        catch
        {
            return;
        }

        foreach (var subDir in subDirs)
        {
            try
            {
                foreach (var file in _fileSystem.EnumerateFiles(subDir, "*", SearchOption.AllDirectories))
                {
                    var ext = GetExtensionForFilter(file);
                    var loc = new SubfolderLocation { ParentPath = rootParent, FullPath = file };
                    if (data.TryGetValue(ext, out var list))
                    {
                        list.Add(loc);
                    }
                    else
                    {
                        data[ext] = new List<SubfolderLocation> { loc };
                    }
                }
            }
            catch
            {
                // Skip inaccessible subtree
            }
        }
    }

    private int CountFilesInSubdirectories(string rootPath, HashSet<string>? extensionFilter)
    {
        try
        {
            var all = _fileSystem.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !string.Equals(Path.GetDirectoryName(f), rootPath, StringComparison.OrdinalIgnoreCase));

            if (extensionFilter != null)
            {
                all = all.Where(f => extensionFilter.Contains(GetExtensionForFilter(f)));
            }

            return all.Count();
        }
        catch
        {
            return 0;
        }
    }

    private static string GetExtensionForFilter(string path)
    {
        var ext = Path.GetExtension(path);
        return string.IsNullOrEmpty(ext) ? "(no extension)" : ext;
    }

    private (int moved, int failed) FlattenFolder(string rootPath, List<ActionHistoryMove> moves, HashSet<string>? extensionFilter, bool removeEmpty)
    {
        int moved = 0, failed = 0;
        List<string> files;
        try
        {
            files = _fileSystem.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories).ToList();
        }
        catch
        {
            return (0, 0);
        }

        foreach (var file in files)
        {
            var parent = Path.GetDirectoryName(file);
            if (string.Equals(parent, rootPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (extensionFilter != null && !extensionFilter.Contains(GetExtensionForFilter(file)))
            {
                continue;
            }

            var destPath = Path.Combine(rootPath, Path.GetFileName(file));
            if (File.Exists(destPath))
            {
                var nameOnly = Path.GetFileNameWithoutExtension(destPath);
                var ext = Path.GetExtension(destPath);
                var i = 2;
                do
                {
                    destPath = Path.Combine(rootPath, $"{nameOnly} ({i}){ext}");
                    i++;
                }
                while (File.Exists(destPath));
            }

            try
            {
                File.Move(file, destPath);
                moves.Add(new ActionHistoryMove { Source = file, Destination = destPath });
                moved++;
            }
            catch
            {
                failed++;
            }
        }

        // Remove empty subdirectories bottom-up (optional)
        if (!removeEmpty)
        {
            return (moved, failed);
        }

        try
        {
            foreach (var sub in _fileSystem.EnumerateDirectories(rootPath).ToList())
            {
                RemoveEmptyDirectoriesRecursive(sub);
            }
        }
        catch
        {
            // Directory enumeration failed — skip cleanup
        }

        return (moved, failed);
    }

    private void RemoveEmptyDirectoriesRecursive(string path)
    {
        try
        {
            foreach (var sub in _fileSystem.EnumerateDirectories(path).ToList())
            {
                RemoveEmptyDirectoriesRecursive(sub);
            }

            if (!_fileSystem.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).Any() &&
                !_fileSystem.EnumerateDirectories(path).Any())
            {
                Directory.Delete(path, recursive: false);
            }
        }
        catch
        {
            // Skip on error
        }
    }

    private async void ScanSubfolders()
    {
        IsScanningFolders = true;
        DiscoveredSubfolders.Clear();
        DiscoveredFileTypes.Clear();
        SubfolderFilter = string.Empty;
        FileTypeFilter = string.Empty;
        OnPropertyChanged(nameof(FilteredSubfolders));
        OnPropertyChanged(nameof(FilteredFileTypes));
        ClearSubfolderStatus = "Scanning folders and files...";

        var selectedFolders = FolderSearchResults.Where(r => r.IsSelected).ToList();
        var includeNested = FolderSearchIncludeSubdirectories;
        var folderPaths = selectedFolders.Select(f => f.FullPath).ToList();

        try
        {
            var (subfolderItems, fileTypeItems) = await Task.Run(() =>
            {
                var subfolders = new Dictionary<string, List<SubfolderLocation>>(StringComparer.OrdinalIgnoreCase);
                var fileTypes = new Dictionary<string, List<SubfolderLocation>>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in folderPaths)
                {
                    ScanFolderContents(path, path, subfolders, fileTypes, includeNested);
                }

                var subItems = subfolders
                    .OrderByDescending(k => k.Value.Count)
                    .ThenBy(k => k.Key)
                    .Select(kvp => new SubfolderItem
                    {
                        Name = kvp.Key,
                        Count = kvp.Value.Count,
                        Locations = kvp.Value,
                        TotalSize = kvp.Value.Sum(loc => GetDirectorySize(loc.FullPath)),
                    })
                    .ToList();

                var typeItems = fileTypes
                    .OrderByDescending(k => k.Value.Count)
                    .ThenBy(k => k.Key)
                    .Select(kvp => new SubfolderItem
                    {
                        Name = kvp.Key,
                        Count = kvp.Value.Count,
                        Locations = kvp.Value,
                        TotalSize = kvp.Value.Sum(loc => SafeFileSize(loc.FullPath)),
                    })
                    .ToList();

                return (subItems, typeItems);
            }).ConfigureAwait(true);

            foreach (var item in subfolderItems)
            {
                DiscoveredSubfolders.Add(item);
            }

            foreach (var item in fileTypeItems)
            {
                DiscoveredFileTypes.Add(item);
            }

            OnPropertyChanged(nameof(FilteredSubfolders));
            OnPropertyChanged(nameof(FilteredFileTypes));
            var totalSubfolderSize = new SubfolderItem { TotalSize = DiscoveredSubfolders.Sum(s => s.TotalSize) }.TotalSizeDisplay;
            var totalFileSize = new SubfolderItem { TotalSize = DiscoveredFileTypes.Sum(t => t.TotalSize) }.TotalSizeDisplay;
            ClearSubfolderStatus = $"Found {DiscoveredSubfolders.Count} subfolder names ({totalSubfolderSize}) and {DiscoveredFileTypes.Count} file types ({totalFileSize}) across {selectedFolders.Count} folders.";
        }
        finally
        {
            IsScanningFolders = false;
        }
    }

    private async System.Threading.Tasks.Task ComputeRestoredSizesAsync()
    {
        var snapshot = FolderSearchResults.ToList();
        var sizes = await System.Threading.Tasks.Task.Run(() =>
            snapshot.ToDictionary(r => r.FullPath, r => GetDirectorySize(r.FullPath))).ConfigureAwait(true);

        foreach (var r in FolderSearchResults)
        {
            if (sizes.TryGetValue(r.FullPath, out var size))
            {
                r.TotalSize = size;
            }
        }
    }

    private static void SetAllToggles(ObservableCollection<ToggleItem> items, bool enabled)
    {
        foreach (var item in items)
        {
            item.IsEnabled = enabled;
        }
    }

    private static void RecycleFile(string path) =>
        VbFileSystem.DeleteFile(path, VbUIOption.OnlyErrorDialogs, VbRecycleOption.SendToRecycleBin);

    private static void RecycleDirectory(string path) =>
        VbFileSystem.DeleteDirectory(path, VbUIOption.OnlyErrorDialogs, VbRecycleOption.SendToRecycleBin);

    private void PushHistory(ActionHistoryEntry entry)
    {
        ActionHistory.Insert(0, entry);
        while (ActionHistory.Count > MaxHistoryEntries)
        {
            ActionHistory.RemoveAt(ActionHistory.Count - 1);
        }

        OnPropertyChanged(nameof(UndoTooltip));
        SaveSettings();
    }

    private void UndoLastAction()
    {
        if (ActionHistory.Count == 0)
        {
            return;
        }

        UndoEntry(ActionHistory[0]);
    }

    private void UndoSpecificAction(ActionHistoryEntry? entry)
    {
        if (entry == null || !ActionHistory.Contains(entry))
        {
            return;
        }

        UndoEntry(entry);
    }

    private void UndoEntry(ActionHistoryEntry entry)
    {
        int restored = 0, failed = 0;

        switch (entry.Kind)
        {
            case ActionHistoryKind.MoveFiles:
                // Reverse moves in REVERSE order so later (2) conflict resolution unwinds correctly
                for (int i = entry.Moves.Count - 1; i >= 0; i--)
                {
                    var src = entry.Moves[i].Source;
                    var dst = entry.Moves[i].Destination;
                    try
                    {
                        if (File.Exists(src))
                        {
                            failed++;
                            continue;
                        }

                        if (!Directory.Exists(Path.GetDirectoryName(src) ?? string.Empty))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(src)!);
                        }

                        File.Move(dst, src);
                        restored++;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                StatusMessage = failed == 0
                    ? $"Undone: restored {restored} files to their original folders."
                    : $"Undone: {restored} restored, {failed} failed (destination occupied or locked).";
                break;

            case ActionHistoryKind.RecycleFiles:
            case ActionHistoryKind.RecycleDirectories:
                restored = RestoreFromRecycleBin(entry.RecycledPaths);
                failed = entry.RecycledPaths.Count - restored;
                StatusMessage = failed == 0
                    ? $"Undone: restored {restored} item(s) from Recycle Bin."
                    : $"Undone: {restored} restored, {failed} not found in Recycle Bin (may have been emptied).";
                break;
        }

        ActionHistory.Remove(entry);
        OnPropertyChanged(nameof(UndoTooltip));
        SaveSettings();
    }

    private void ClearHistory()
    {
        ActionHistory.Clear();
        OnPropertyChanged(nameof(UndoTooltip));
        StatusMessage = "History cleared (files were not affected).";
        SaveSettings();
    }

    private void RaiseHistoryAnalytics()
    {
        OnPropertyChanged(nameof(HistoryTotalEntries));
        OnPropertyChanged(nameof(HistoryMoveOperationCount));
        OnPropertyChanged(nameof(HistoryMoveItemCount));
        OnPropertyChanged(nameof(HistoryRecycleFileOperationCount));
        OnPropertyChanged(nameof(HistoryRecycleFileItemCount));
        OnPropertyChanged(nameof(HistoryRecycleDirOperationCount));
        OnPropertyChanged(nameof(HistoryRecycleDirItemCount));
    }

    private static int RestoreFromRecycleBin(IEnumerable<string> originalPaths)
    {
        var wanted = new HashSet<string>(originalPaths, StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
        {
            return 0;
        }

        int restored = 0;
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
            {
                return 0;
            }

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null)
            {
                return 0;
            }

            dynamic bin = shell.NameSpace(10); // ssfBITBUCKET
            if (bin == null)
            {
                return 0;
            }

            dynamic items = bin.Items();
            int count = items.Count;

            // Iterate in reverse — invoking Restore removes the item from the bin
            for (int i = count - 1; i >= 0; i--)
            {
                dynamic item = items.Item(i);
                string? originalFolder = bin.GetDetailsOf(item, 1) as string; // column 1 = Original Location
                string? name = item.Name as string;
                if (string.IsNullOrEmpty(originalFolder) || string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var fullOriginal = Path.Combine(originalFolder, name);
                if (!wanted.Contains(fullOriginal))
                {
                    continue;
                }

                try
                {
                    item.InvokeVerb("&Restore");
                    restored++;
                }
                catch
                {
                    // Skip items that can't be restored
                }
            }
        }
        catch
        {
            // Shell unavailable or access denied
        }

        return restored;
    }

    private long SafeFileSize(string path)
    {
        try
        {
            return _fileSystem.GetFileSize(path);
        }
        catch
        {
            return 0L;
        }
    }

    private long GetDirectorySize(string path) => GetDirectorySize(path, System.Threading.CancellationToken.None);

    private long GetDirectorySize(string path, System.Threading.CancellationToken token)
    {
        try
        {
            long total = 0;
            foreach (var file in _fileSystem.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                token.ThrowIfCancellationRequested();
                total += SafeFileSize(file);
            }

            return total;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return 0L;
        }
    }

    private void ScanFolderContents(
        string path,
        string rootParent,
        Dictionary<string, List<SubfolderLocation>> subfolderData,
        Dictionary<string, List<SubfolderLocation>> fileTypeData,
        bool recurse)
    {
        try
        {
            foreach (var subDir in _fileSystem.EnumerateDirectories(path))
            {
                var name = Path.GetFileName(subDir);
                var location = new SubfolderLocation
                {
                    ParentPath = rootParent,
                    FullPath = subDir,
                };

                if (subfolderData.TryGetValue(name, out var locations))
                {
                    locations.Add(location);
                }
                else
                {
                    subfolderData[name] = new List<SubfolderLocation> { location };
                }

                if (recurse)
                {
                    ScanFolderContents(subDir, rootParent, subfolderData, fileTypeData, true);
                }
            }

            foreach (var file in _fileSystem.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
            {
                var ext = Path.GetExtension(file);
                if (string.IsNullOrEmpty(ext))
                {
                    ext = "(no extension)";
                }

                var fileLocation = new SubfolderLocation
                {
                    ParentPath = rootParent,
                    FullPath = file,
                };

                if (fileTypeData.TryGetValue(ext, out var fileLocations))
                {
                    fileLocations.Add(fileLocation);
                }
                else
                {
                    fileTypeData[ext] = new List<SubfolderLocation> { fileLocation };
                }
            }
        }
        catch
        {
            // Access denied — skip
        }
    }

    private void ClearSelectedSubfolders()
    {
        var selectedSubs = DiscoveredSubfolders.Where(s => s.IsSelected).Select(s => s.Name).ToList();
        var selectedFolders = FolderSearchResults.Where(r => r.IsSelected).ToList();

        if (selectedSubs.Count == 0 || selectedFolders.Count == 0)
        {
            return;
        }

        // Count how many will be affected
        int totalToDelete = 0;
        foreach (var folder in selectedFolders)
        {
            foreach (var subName in selectedSubs)
            {
                var subPath = Path.Combine(folder.FullPath, subName);
                if (Directory.Exists(subPath))
                {
                    totalToDelete++;
                }
            }
        }

        if (totalToDelete == 0)
        {
            ClearSubfolderStatus = "No matching subfolders found to delete.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Send {totalToDelete} subfolders ({string.Join(", ", selectedSubs)}) from {selectedFolders.Count} selected folders to the Recycle Bin?\n\nYou can restore them from the Recycle Bin if needed.",
            "Confirm Recycle Subfolders",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        int deleted = 0;
        int failed = 0;
        var recycled = new List<string>();
        foreach (var folder in selectedFolders)
        {
            foreach (var subName in selectedSubs)
            {
                var subPath = Path.Combine(folder.FullPath, subName);
                if (!Directory.Exists(subPath))
                {
                    continue;
                }

                try
                {
                    RecycleDirectory(subPath);
                    recycled.Add(subPath);
                    deleted++;
                }
                catch (Exception ex)
                {
                    failed++;
                    System.Windows.MessageBox.Show(
                        $"Failed to recycle '{subPath}':\n{ex.Message}",
                        "Delete Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        if (recycled.Count > 0)
        {
            PushHistory(new ActionHistoryEntry
            {
                Kind = ActionHistoryKind.RecycleDirectories,
                RecycledPaths = recycled,
                Summary = $"Recycled {recycled.Count} subfolders ({string.Join(", ", selectedSubs)})",
            });
        }

        ClearSubfolderStatus = $"Cleared {deleted} subfolders." + (failed > 0 ? $" {failed} failed." : string.Empty);

        // Re-scan to update counts
        ScanSubfolders();
    }

    private void SelectAllSubfolders()
    {
        foreach (var s in FilteredSubfolders)
        {
            s.IsSelected = true;
        }
    }

    private void ClearSubfolderSelection()
    {
        foreach (var s in DiscoveredSubfolders)
        {
            s.IsSelected = false;
        }
    }

    private void SelectAllFileTypes()
    {
        foreach (var t in FilteredFileTypes)
        {
            t.IsSelected = true;
        }
    }

    private void ClearFileTypeSelection()
    {
        foreach (var t in DiscoveredFileTypes)
        {
            t.IsSelected = false;
        }
    }

    private void ClearSelectedFileTypes()
    {
        var selected = DiscoveredFileTypes.Where(t => t.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var totalFiles = selected.Sum(t => t.Locations.Count);
        var typeList = string.Join(", ", selected.Select(t => t.Name));

        var result = System.Windows.MessageBox.Show(
            $"Send {totalFiles} files of type(s) [{typeList}] to the Recycle Bin?\n\nYou can restore them from the Recycle Bin if needed.",
            "Confirm Recycle Files",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = 0;
        var failed = 0;
        var recycled = new List<string>();
        foreach (var type in selected)
        {
            foreach (var loc in type.Locations)
            {
                try
                {
                    RecycleFile(loc.FullPath);
                    recycled.Add(loc.FullPath);
                    deleted++;
                }
                catch
                {
                    failed++;
                }
            }
        }

        if (recycled.Count > 0)
        {
            var recycledTypeList = string.Join(", ", selected.Select(t => t.Name));
            PushHistory(new ActionHistoryEntry
            {
                Kind = ActionHistoryKind.RecycleFiles,
                RecycledPaths = recycled,
                Summary = $"Recycled {recycled.Count} files ({recycledTypeList})",
            });
        }

        ClearSubfolderStatus = failed == 0
            ? $"Sent {deleted} files to Recycle Bin."
            : $"Sent {deleted} files to Recycle Bin. {failed} failed (access denied or in use).";

        ScanSubfolders();
    }

    // ── ACTION METHODS ──
    private void DeleteSelectedFiles()
    {
        var filesToDelete = DuplicateGroups
            .SelectMany(g => g.Files.Where(f => f.IsFileSelected).Select(f => new { Group = g, File = f }))
            .ToList();

        if (filesToDelete.Count == 0)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Send {filesToDelete.Count} selected files to the Recycle Bin?",
            "Confirm Recycle",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = 0;
        var failed = 0;

        var recycled = new List<string>();
        foreach (var item in filesToDelete)
        {
            try
            {
                RecycleFile(item.File.FilePath);
                recycled.Add(item.File.FilePath);
                item.Group.Files.Remove(item.File);
                deleted++;
            }
            catch
            {
                failed++;
            }
        }

        // Remove groups with 0 or 1 file left
        foreach (var group in DuplicateGroups.Where(g => g.Files.Count <= 1).ToList())
        {
            DuplicateGroups.Remove(group);
        }

        if (recycled.Count > 0)
        {
            PushHistory(new ActionHistoryEntry
            {
                Kind = ActionHistoryKind.RecycleFiles,
                RecycledPaths = recycled,
                Summary = $"Recycled {recycled.Count} duplicate files",
            });
        }

        ClosePreview();
        RefreshSelectedFileCount();
        StatusMessage = $"Sent {deleted} files to Recycle Bin" + (failed > 0 ? $", {failed} failed" : string.Empty);
    }

    private void MoveSelectedFiles()
    {
        if (string.IsNullOrWhiteSpace(MoveTargetPath))
        {
            StatusMessage = "Set a move target path first.";
            return;
        }

        if (!EnsureMoveTargetDirectory())
        {
            return;
        }

        var filesToMove = DuplicateGroups
            .SelectMany(g => g.Files.Where(f => f.IsFileSelected).Select(f => new { Group = g, File = f }))
            .ToList();

        if (filesToMove.Count == 0)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Move {filesToMove.Count} selected files to:\n{MoveTargetPath}",
            "Confirm Move",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        var failed = 0;
        var moves = new List<ActionHistoryMove>();

        foreach (var item in filesToMove)
        {
            var destPath = MoveFileToTarget(item.File.FilePath);
            if (destPath != null)
            {
                moves.Add(new ActionHistoryMove { Source = item.File.FilePath, Destination = destPath });
                item.Group.Files.Remove(item.File);
            }
            else
            {
                failed++;
            }
        }

        // Remove groups with 0 or 1 file left
        foreach (var group in DuplicateGroups.Where(g => g.Files.Count <= 1).ToList())
        {
            DuplicateGroups.Remove(group);
        }

        if (moves.Count > 0)
        {
            PushHistory(new ActionHistoryEntry
            {
                Kind = ActionHistoryKind.MoveFiles,
                Moves = moves,
                Summary = $"Moved {moves.Count} files to {MoveTargetPath}",
            });
        }

        ClosePreview();
        RefreshSelectedFileCount();
        StatusMessage = $"Moved {moves.Count} files to {MoveTargetPath}" + (failed > 0 ? $", {failed} failed" : string.Empty);
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

    private string? MoveFileToTarget(string sourcePath)
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
            return destPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Refreshes the selected file count from IsFileSelected flags.
    /// </summary>
    public void RefreshSelectedFileCount()
    {
        SelectedFileCount = DuplicateGroups
            .SelectMany(g => g.Files)
            .Count(f => f.IsFileSelected);
    }

    private void SelectAllInGroup(DuplicateGroup? group)
    {
        if (group == null)
        {
            return;
        }

        foreach (var file in group.Files)
        {
            file.IsFileSelected = true;
        }

        RefreshSelectedFileCount();
    }

    private void ClearSelectionInGroup(DuplicateGroup? group)
    {
        if (group == null)
        {
            return;
        }

        foreach (var file in group.Files)
        {
            file.IsFileSelected = false;
        }

        RefreshSelectedFileCount();
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
