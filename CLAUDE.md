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
│       │   ├── MainViewModel.cs
│       │   ├── ViewModelBase.cs
│       │   ├── ExtensionFilter.cs
│       │   └── ToggleItem.cs          # Enable/disable wrapper for paths & exclusions
│       ├── Views/
│       └── Helpers/
│           ├── FormattedTextBehavior.cs  # Rich text markup parser (<b>,<h>,<w>,<link>)
│           └── ...
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
- **ToggleItem pattern**: Target paths and exclude folders use `ToggleItem` wrapper (string + IsEnabled) for temporary enable/disable
- **FilterRule INotifyPropertyChanged**: `FilterRule.IsEnabled` notifies UI for bulk enable/disable operations
- **Save on change**: `SaveSettings()` called on every mutation (add/remove/reorder rules, paths, exclusions) — not just on window close
- **`[JsonIgnore]` on computed properties**: Getter-only properties on serialized models (e.g., `DisplaySummary`, `Priority`) must have `[System.Text.Json.Serialization.JsonIgnore]` to prevent serialization/deserialization issues with old settings files
- **Enum rename safety**: `System.Text.Json` serializes enums as integers by default. When renaming enum values (e.g., `Select` → `Contains`), keep the same ordinal position to maintain backward compatibility with existing settings

---

## Reusable Features

### Contextual Help Button (`?` Popup)
- **Style**: `HelpButtonStyle` in `Window.Resources` — 16px `?` circle with click-to-open popup
- **Behavior**: `Helpers/FormattedTextBehavior.cs` — parses `Tag` markup into styled `Inline` elements
- **Markup tags**: `<b>bold</b>`, `<h>heading</h>`, `<w>warning</w>`, `<link=URL>text</link>`
- **Links**: `<link>` tag creates clickable `Hyperlink` that opens URL in default browser (e.g., regex101.com)
- **Spec**: See `D:\Programing\claude-prompt-solution-architect\prompts\10-development\feature-common\CONTEXTUAL_HELP_BUTTON.md`

### Inline WrapPanel Layout Pattern
- **Pattern**: All filter/action/exclude sections use `WrapPanel` with grouped `StackPanel` sub-panels
- **Spacing**: Each sub-panel has `Margin="0,2,0,2"` for vertical gap when wrapping to a second row
- **Structure**: `Title (?) | controls | actions` — separated by `Border Width="1"` dividers
- **Sections using this**: Base Filters, Custom Rules, Exclude Folders, Action
- **Overflow**: Wraps to multiple rows automatically — no scrollbars, no collapsible panels

### Window State Persistence
- **Saves on close**: window position, size, and maximized state to `settings.json` via `AppSettings` (also saved on every settings change)
- **Restores on load**: `MainWindow.Loaded` reads settings, validates position is on-screen via `SystemParameters.VirtualScreen*`
- **Fallback**: if saved position is off-screen (monitor unplugged/changed), defaults to `CenterScreen`
- **Maximized**: uses `RestoreBounds` to save normal size even when closing maximized
- **Spec**: See `D:\Programing\claude-prompt-solution-architect\prompts\10-development\feature-common\WINDOW_STATE_PERSISTENCE.md`

---

## Quality Gates (CI)

1. `dotnet build -c Release` — Compile + StyleCop + .NET Analyzers
2. `dotnet test --collect:"XPlat Code Coverage" --settings tests/WindowsFileManager.Tests/coverlet.runsettings` — All tests pass + 100% coverage
3. `dotnet format --verify-no-changes` — EditorConfig compliance

---

## Release Notes

- **Format**: Keep a Changelog (`CHANGELOG.md`)
- **Versioning**: Semantic Versioning (MAJOR.MINOR.PATCH)
- **Auto-generate**: From conventional commits, review before commit
- **Publish**: GitHub Releases on tag push
- **Version files**: `src/WindowsFileManager/Package.appxmanifest` (Identity Version, 4-part: `x.y.z.0`)
- **Spec**: See solution-architect `feature-common/RELEASE_NOTES.md`

---

## Project Notes

- **[2026-04-15]** `Stop-Process -Force` kills the app without triggering `Window.Closing`, so settings were lost. Fixed by saving settings on every mutation, not just on close.
- **[2026-04-15]** Filter UI refactored from collapsible panels to always-visible inline WrapPanel rows. No more expand/collapse toggles — everything visible at a glance, wraps responsively.
- **[2026-04-15]** `FilterAction.Select` renamed to `FilterAction.Contains` for user clarity. Backward compatible with old settings (enum ordinal 0 unchanged).
- **[2026-04-14]** `dotnet watch run` does NOT work for WPF apps (watch/hot-reload is web-only). Use `dotnet run` from `src/WindowsFileManager/` to launch the app during development.
