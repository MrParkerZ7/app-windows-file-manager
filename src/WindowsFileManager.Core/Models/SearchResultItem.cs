namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a single search result (file or folder).
/// </summary>
public class SearchResultItem
{
    /// <summary>
    /// Gets or sets the full path of the result.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name (file or folder name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent directory path.
    /// </summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result type (File or Folder).
    /// </summary>
    public string ResultType { get; set; } = "File";

    /// <summary>
    /// Gets or sets the file size in bytes (0 for folders).
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the formatted file size string.
    /// </summary>
    public string FormattedSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file extension (empty for folders).
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets the icon for this result type.
    /// </summary>
    public string Icon => ResultType == "Folder" ? "📁" : "📄";
}
