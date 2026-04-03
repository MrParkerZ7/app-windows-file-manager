namespace WindowsFileManager.Models;

/// <summary>
/// A group of files that share the same content (identical hash).
/// </summary>
public class DuplicateGroup
{
    /// <summary>
    /// Gets or sets the shared hash of all files in this group.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size shared by all duplicates.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the list of duplicate files.
    /// </summary>
    public List<ScannedFile> Files { get; set; } = new();

    /// <summary>
    /// Gets the number of duplicate files in this group.
    /// </summary>
    public int Count => Files.Count;

    /// <summary>
    /// Gets the wasted space (all copies except one).
    /// </summary>
    public long WastedBytes => FileSize * (Count - 1);

    /// <summary>
    /// Gets the human-readable wasted space.
    /// </summary>
    public string FormattedWastedSize => ScannedFile.FormatFileSize(WastedBytes);

    /// <summary>
    /// Gets the human-readable file size.
    /// </summary>
    public string FormattedFileSize => ScannedFile.FormatFileSize(FileSize);
}
