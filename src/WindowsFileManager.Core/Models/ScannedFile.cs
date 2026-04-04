namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a file discovered during scanning.
/// </summary>
public class ScannedFile
{
    /// <summary>
    /// Gets or sets the full path to the file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name without path.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the computed hash of the file content.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last modified date of the file.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets the human-readable file size.
    /// </summary>
    public string FormattedSize => FormatFileSize(FileSize);

    /// <summary>
    /// Formats a byte count into a human-readable size string.
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        if (bytes < 1024 * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
