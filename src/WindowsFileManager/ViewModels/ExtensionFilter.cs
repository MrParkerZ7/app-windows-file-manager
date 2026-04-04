using System.Diagnostics.CodeAnalysis;

namespace WindowsFileManager.ViewModels;

/// <summary>
/// Represents a file extension filter toggle.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExtensionFilter : ViewModelBase
{
    private bool _isChecked = true;
    private int _fileCount;
    private long _totalSize;

    /// <summary>
    /// Gets or sets the file extension (e.g., ".jpg").
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this extension is visible.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    /// <summary>
    /// Gets or sets the number of duplicate groups with this extension.
    /// </summary>
    public int FileCount
    {
        get => _fileCount;
        set => SetProperty(ref _fileCount, value);
    }

    /// <summary>
    /// Gets or sets the total size of files with this extension.
    /// </summary>
    public long TotalSize
    {
        get => _totalSize;
        set => SetProperty(ref _totalSize, value);
    }

    /// <summary>
    /// Gets formatted total size.
    /// </summary>
    public string FormattedSize => Core.Models.ScannedFile.FormatFileSize(TotalSize);
}
