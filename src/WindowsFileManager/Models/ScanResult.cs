namespace WindowsFileManager.Models;

/// <summary>
/// The result of a duplicate file scan.
/// </summary>
public class ScanResult
{
    /// <summary>
    /// Gets or sets the total number of files scanned.
    /// </summary>
    public int TotalFilesScanned { get; set; }

    /// <summary>
    /// Gets or sets the total number of duplicate files found.
    /// </summary>
    public int TotalDuplicates { get; set; }

    /// <summary>
    /// Gets or sets the total wasted space in bytes.
    /// </summary>
    public long TotalWastedBytes { get; set; }

    /// <summary>
    /// Gets or sets the list of duplicate groups.
    /// </summary>
    public List<DuplicateGroup> DuplicateGroups { get; set; } = new();

    /// <summary>
    /// Gets or sets the scan duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets the human-readable total wasted space.
    /// </summary>
    public string FormattedWastedSize => ScannedFile.FormatFileSize(TotalWastedBytes);
}
