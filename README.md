# Windows File Manager

A .NET 8 WPF desktop application for managing files on Windows. The first feature is **duplicate file detection** — scan one or more folders, find identical files by content hash, and reclaim wasted disk space.

## Features

- **Duplicate File Detection** — SHA256 content hashing with a three-stage filter (size grouping → hash computation → duplicate confirmation) for fast scanning
- **Multi-Folder Scanning** — Add folders by typing paths or browsing, scan across multiple directories simultaneously, detect cross-folder duplicates
- **Overlapping Path Deduplication** — Adding both `D:\` and `D:\subfolder` won't produce false duplicates
- **Analytics Dashboard** — Real-time statistics: total files, duplicates found, groups, scan time, wasted space %, top duplicate extensions, size distribution chart
- **File Actions** — Open file location in Explorer or delete duplicates with confirmation dialog
- **Settings Persistence** — Target folders, subdirectory preference, and settings saved to `%APPDATA%/WindowsFileManager/settings.json` and restored on startup
- **Cancellation Support** — Cancel long-running scans at any time
- **Progress Reporting** — Live file count with throttled UI updates (every 100 files)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Windows 10/11 (WPF requires Windows)

## Getting Started

```bash
# Clone the repository
git clone <repo-url>
cd app-window-file-manager

# Build
dotnet build

# Run the application
dotnet run --project src/WindowsFileManager

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --settings tests/WindowsFileManager.Tests/coverlet.runsettings
```

## Project Structure

The solution follows **Clean Architecture** with four modules:

```
WindowsFileManager/
├── src/
│   ├── WindowsFileManager.Core/           # Models + Interfaces (zero dependencies)
│   │   ├── Models/
│   │   │   ├── ScannedFile.cs             # File metadata with formatted size
│   │   │   ├── DuplicateGroup.cs          # Group of identical files
│   │   │   ├── ScanOptions.cs             # Scan configuration (paths, filters)
│   │   │   ├── ScanResult.cs              # Scan output with statistics
│   │   │   ├── ScanAnalytics.cs           # Computed analytics, extensions, size buckets
│   │   │   └── AppSettings.cs             # Persisted user preferences
│   │   └── Services/
│   │       └── IFileSystemService.cs      # File system abstraction
│   │
│   ├── WindowsFileManager.Application/    # Business logic (depends on Core only)
│   │   └── Services/
│   │       ├── DuplicateScannerService.cs # Duplicate detection algorithm
│   │       ├── FileHashService.cs         # SHA256 file hashing
│   │       └── SettingsService.cs         # JSON settings persistence
│   │
│   ├── WindowsFileManager.Infrastructure/ # Real implementations (depends on Core only)
│   │   └── Services/
│   │       └── FileSystemService.cs       # System.IO file operations
│   │
│   └── WindowsFileManager/               # WPF UI layer (depends on all modules)
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs           # INotifyPropertyChanged base
│       │   └── MainViewModel.cs           # Main window state + commands
│       ├── Views/
│       │   ├── MainWindow.xaml            # UI layout
│       │   └── MainWindow.xaml.cs         # Window code-behind
│       └── Helpers/
│           ├── RelayCommand.cs            # ICommand implementation
│           ├── Converters.cs              # Bool/Visibility, Percent/Width converters
│           └── TextBoxEnterKeyBehavior.cs # Enter key attached behavior
│
├── tests/
│   └── WindowsFileManager.Tests/          # Unit tests (91 tests, 100% line coverage)
│       ├── Models/                        # Core model tests
│       ├── Services/                      # Application service tests with Moq
│       └── Helpers/                       # UI helper tests
│
├── Directory.Build.props                  # Shared analyzers (StyleCop, .NET Analyzers)
├── .editorconfig                          # Code style rules
├── stylecop.json                          # StyleCop configuration
└── .github/workflows/ci.yml              # GitHub Actions CI pipeline
```

### Dependency Flow

```
UI (WindowsFileManager) → Application → Core ← Infrastructure
```

- **Core** has zero dependencies — models and interfaces only
- **Application** depends on Core interfaces, not implementations
- **Infrastructure** implements Core interfaces with real I/O
- **UI** wires everything together

## Architecture

### Duplicate Detection Algorithm

The scanner uses a three-stage filter for efficiency:

1. **Collect & Filter** — Enumerate files across all target paths, apply size/extension filters, deduplicate overlapping paths
2. **Group by Size** — Files with unique sizes cannot be duplicates (O(n) filter eliminates most files)
3. **Hash Same-Size Files** — Only compute SHA256 for files that share a size, then group by hash

This avoids hashing every file — the expensive operation only runs on candidates.

### MVVM Pattern

- **Models** — Pure data classes in Core, no UI dependencies
- **ViewModels** — Bind to Views via `INotifyPropertyChanged`, expose `ICommand` for actions
- **Views** — XAML-only UI, code-behind limited to window lifecycle events

### Testability

All file system operations go through `IFileSystemService`, allowing complete mock-based testing without touching the real file system. The real implementation (`FileSystemService`) is isolated in the Infrastructure module and excluded from coverage.

## Testing

```bash
# Run all 91 tests with coverage report
dotnet test --collect:"XPlat Code Coverage" \
  --settings tests/WindowsFileManager.Tests/coverlet.runsettings

# Quick run without coverage
dotnet test
```

**Coverage:** 100% line, 96% branch, 100% method across Core + Application + Helpers.

| Module | Line | Branch | Method |
|--------|------|--------|--------|
| WindowsFileManager.Core | 100% | 100% | 100% |
| WindowsFileManager.Application | 100% | 94% | 100% |
| WindowsFileManager (Helpers) | 100% | 96% | 100% |

**Test stack:** xUnit + Moq + FluentAssertions

## CI/CD

GitHub Actions pipeline (`.github/workflows/ci.yml`) runs on every push and PR to `main`:

1. **Build** — `dotnet build -c Release`
2. **Format Check** — `dotnet format --verify-no-changes`
3. **Test + Coverage** — All tests with coverlet.collector, coverage report uploaded as artifact

## Code Quality

- **StyleCop Analyzers** — Enforced via `Directory.Build.props`
- **EditorConfig** — 4-space indentation, file-scoped namespaces, PascalCase methods, `_camelCase` fields
- **.NET Analyzers** — Latest analysis level enabled

## License

This project is for personal use.
