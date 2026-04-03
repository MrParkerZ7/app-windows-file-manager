namespace WindowsFileManager.Models;

/// <summary>
/// Options for controlling the duplicate file scan.
/// </summary>
public class ScanOptions
{
    /// <summary>
    /// Gets or sets the list of directory paths to scan.
    /// </summary>
    public List<string> TargetPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to scan subdirectories recursively.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum file size in bytes to consider (skip tiny files).
    /// </summary>
    public long MinimumFileSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets file extension filters (empty = all files).
    /// </summary>
    public List<string> FileExtensions { get; set; } = new();
}
