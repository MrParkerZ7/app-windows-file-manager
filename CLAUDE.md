# CLAUDE.md

> AI agent instructions for this .NET 8 WPF application.

---

## Quick Reference

```
BUILD:    dotnet build -c Release
FORMAT:   dotnet format
TEST:     dotnet test --collect:"XPlat Code Coverage" --settings tests/WindowsFileManager.Tests/coverlet.runsettings
COVERAGE: 100% line coverage (enforced by coverlet.collector + runsettings)
MSIX:     dotnet publish src/WindowsFileManager -c Release -r win-x64 --self-contained -p:WindowsPackageType=MSIX
```

---

## Project Structure (Modular Monorepo)

```
WindowsFileManager/
├── WindowsFileManager.sln
├── Directory.Build.props          # Shared analyzers (StyleCop, .NET Analyzers)
├── .editorconfig                  # Code style rules
├── stylecop.json                  # StyleCop config
├── src/
│   ├── WindowsFileManager.Core/           # Models + Interfaces (zero dependencies)
│   │   ├── Models/
│   │   └── Services/IFileSystemService.cs
│   ├── WindowsFileManager.Application/    # Business logic (depends on Core)
│   │   └── Services/
│   ├── WindowsFileManager.Infrastructure/ # Real I/O implementations (depends on Core)
│   │   └── Services/FileSystemService.cs
│   └── WindowsFileManager/               # WPF UI (depends on all)
│       ├── ViewModels/
│       ├── Views/
│       └── Helpers/
└── tests/
    └── WindowsFileManager.Tests/
        ├── Models/                # Core model tests
        ├── Services/              # Application service tests with Moq
        └── Helpers/               # UI helper tests
```

**Dependency flow:** `UI → Application → Core ← Infrastructure`

---

## Architecture: Clean Architecture + MVVM

- **Core**: Pure models + interfaces, no dependencies — shareable across modules
- **Application**: Business logic services depending only on Core interfaces
- **Infrastructure**: Real file system implementation, excluded from coverage
- **UI (WindowsFileManager)**: WPF Views, ViewModels, Helpers — wires everything via DI

---

## Key Conventions

- **Naming**: PascalCase methods/properties, `_camelCase` private fields, `I` prefix interfaces
- **Nullable**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **File-scoped namespaces**: Required (`namespace Foo;`)
- **Testing**: xUnit + Moq + FluentAssertions, AAA pattern
- **Coverage exclusions**: Views, ViewModels, Infrastructure, generated code (via runsettings Include filter)
- **Interface abstraction**: All I/O through `IFileSystemService` for mock-friendly testing

---

## Quality Gates (CI)

1. `dotnet build -c Release` — Compile + StyleCop + .NET Analyzers
2. `dotnet test --collect:"XPlat Code Coverage" --settings tests/WindowsFileManager.Tests/coverlet.runsettings` — All tests pass + 100% coverage
3. `dotnet format --verify-no-changes` — EditorConfig compliance

---

## Project Notes

- **[2026-04-14]** `dotnet watch run` does NOT work for WPF apps (watch/hot-reload is web-only). Use `dotnet run` from `src/WindowsFileManager/` to launch the app during development.
