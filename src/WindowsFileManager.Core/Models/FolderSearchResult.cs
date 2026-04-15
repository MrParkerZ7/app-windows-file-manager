namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a folder found during folder search.
/// </summary>
public class FolderSearchResult
{
    /// <summary>
    /// Gets or sets the full path of the folder.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the folder name.
    /// </summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent directory path.
    /// </summary>
    public string ParentPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the matched search pattern.
    /// </summary>
    public string MatchedPattern { get; set; } = string.Empty;
}
