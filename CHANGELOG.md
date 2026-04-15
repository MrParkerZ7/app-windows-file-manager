# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com),
and this project adheres to [Semantic Versioning](https://semver.org).

## [Unreleased]

## [1.0.0] - 2026-04-15

Initial release.

### Added
- Duplicate file finder with multi-folder scan and hash-based detection
- Modular monorepo with Clean Architecture layers (Core, Application, Infrastructure, UI)
- File preview panel with image, video, audio, and text support
- Mini thumbnail previews in duplicate group list
- Analytics dashboard with scan statistics and storage insights
- Dynamic filter rules with regex pattern matching and priority ordering
- Extension type filters with select all/clear controls
- Size and duplicate count filters with apply button
- Inline filter UI with responsive WrapPanel layout
- Contextual help buttons (`?` popups) with rich text and clickable links
- Bulk file management: delete, move, select all/newer/older
- Granular move options: move by oldest, newest, filename, or path
- Exclude folders from scan by name
- Collapsible sections for filters and actions
- Window state persistence (position, size, maximized) across sessions
- Live resource monitor (CPU, memory) during scan
- Tabbed UI with full-height Analytics and File Preview panels
- Settings persistence to `settings.json` (paths, filters, rules, preferences)
- GitHub Actions CI pipeline (build, format, test)
- MSIX packaging and Microsoft Store CI/CD pipeline
- 100% test coverage on Core and Application layers (xUnit + Moq + FluentAssertions)
