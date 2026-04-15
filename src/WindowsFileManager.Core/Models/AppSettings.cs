namespace WindowsFileManager.Core.Models;

/// <summary>
/// Persisted application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the list of target folder paths.
    /// </summary>
    public List<string> TargetPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to include subdirectories.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum file size filter.
    /// </summary>
    public long MinimumFileSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether mini preview is enabled.
    /// </summary>
    public bool IsMiniPreview { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether full preview panel is enabled.
    /// </summary>
    public bool IsAutoPreview { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether auto play is enabled.
    /// </summary>
    public bool IsAutoPlay { get; set; }

    /// <summary>
    /// Gets or sets the selected sort option.
    /// </summary>
    public string SelectedSortOption { get; set; } = "Size (largest)";

    /// <summary>
    /// Gets or sets the volume level (0.0 to 1.0).
    /// </summary>
    public double Volume { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the move target path.
    /// </summary>
    public string MoveTargetPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the folder names to exclude from scanning.
    /// </summary>
    public List<string> ExcludeFolderNames { get; set; } = new();

    /// <summary>
    /// Gets or sets the dynamic filter rules.
    /// </summary>
    public List<FilterRule> FilterRules { get; set; } = new();

    /// <summary>
    /// Gets or sets the folder search patterns.
    /// </summary>
    public List<FolderSearchPattern> FolderSearchPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the window left position.
    /// </summary>
    public double? WindowLeft { get; set; }

    /// <summary>
    /// Gets or sets the window top position.
    /// </summary>
    public double? WindowTop { get; set; }

    /// <summary>
    /// Gets or sets the window width.
    /// </summary>
    public double? WindowWidth { get; set; }

    /// <summary>
    /// Gets or sets the window height.
    /// </summary>
    public double? WindowHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the window was maximized.
    /// </summary>
    public bool IsMaximized { get; set; }
}
