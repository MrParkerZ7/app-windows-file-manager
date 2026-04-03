namespace WindowsFileManager.Models;

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
}
