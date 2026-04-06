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
    /// Gets or sets the filename contain filter text.
    /// </summary>
    public string FilenameFilterText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether filename filter uses regex.
    /// </summary>
    public bool IsFilenameRegex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether filename filter ignores case.
    /// </summary>
    public bool IsFilenameIgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets the filepath contain filter text.
    /// </summary>
    public string FilepathFilterText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether filepath filter uses regex.
    /// </summary>
    public bool IsFilepathRegex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether filepath filter ignores case.
    /// </summary>
    public bool IsFilepathIgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets the ignore filename filter text.
    /// </summary>
    public string IgnoreFilenameFilterText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether ignore filename filter uses regex.
    /// </summary>
    public bool IsIgnoreFilenameRegex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ignore filename filter ignores case.
    /// </summary>
    public bool IsIgnoreFilenameIgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets the ignore filepath filter text.
    /// </summary>
    public string IgnoreFilepathFilterText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether ignore filepath filter uses regex.
    /// </summary>
    public bool IsIgnoreFilepathRegex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ignore filepath filter ignores case.
    /// </summary>
    public bool IsIgnoreFilepathIgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the contain filter section is visible.
    /// </summary>
    public bool IsContainSectionVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the ignore filter section is visible.
    /// </summary>
    public bool IsIgnoreSectionVisible { get; set; } = true;
}
