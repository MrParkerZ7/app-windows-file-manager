# CLAUDE.md

> AI agent instructions for this .NET 8 WPF application.

---

## Quick Reference

```
BUILD:    dotnet build -c Release          # Full build
FORMAT:   dotnet format                    # Fix formatting (REQUIRED before commit)
TEST:     dotnet test -c Release           # Run tests with 100% coverage enforcement
COVERAGE: 100% line coverage               # Enforced by Coverlet (threshold in .csproj)
```

---

## Project Structure

```
WindowsFileManager/
├── WindowsFileManager.sln
├── Directory.Build.props          # Shared analyzers (StyleCop, .NET Analyzers)
├── .editorconfig                  # Code style rules
├── stylecop.json                  # StyleCop config
├── src/
│   └── WindowsFileManager/
│       ├── Models/                # Data models (ScannedFile, DuplicateGroup, etc.)
│       ├── Services/              # Business logic (DuplicateScannerService, etc.)
│       ├── ViewModels/            # MVVM ViewModels
│       ├── Views/                 # WPF XAML views
│       └── Helpers/               # RelayCommand, Converters
└── tests/
    └── WindowsFileManager.Tests/
        ├── Models/                # Model tests (mirror source structure)
        ├── Services/              # Service tests with Moq
        └── Helpers/               # Helper tests
```

---

## Architecture: MVVM + Services

- **Models**: Pure data classes, no dependencies
- **Services**: Business logic with `IFileSystemService` abstraction for testability
- **ViewModels**: UI state management, binds to Views via INotifyPropertyChanged
- **Views**: XAML only, code-behind excluded from coverage

---

## Key Conventions

- **Naming**: PascalCase methods/properties, `_camelCase` private fields, `I` prefix interfaces
- **Nullable**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **File-scoped namespaces**: Required (`namespace Foo;`)
- **Testing**: xUnit + Moq + FluentAssertions, AAA pattern
- **Coverage exclusions**: Views, ViewModels, FileSystemService, generated code
- **Interface abstraction**: All I/O through `IFileSystemService` for mock-friendly testing

---

## Quality Gates (CI)

1. `dotnet build -c Release` — Compile + StyleCop + .NET Analyzers
2. `dotnet test -c Release` — All tests pass + 100% coverage
3. `dotnet format --verify-no-changes` — EditorConfig compliance
