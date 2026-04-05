using System.ComponentModel;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// A group of files that share the same content (identical hash).
/// </summary>
public class DuplicateGroup : INotifyPropertyChanged
{
    private bool _isSelected;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets a value indicating whether this group is selected for bulk actions.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

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
    /// Gets the file path of the first file in this group (for preview).
    /// </summary>
    public string? FirstFilePath => Files.Count > 0 ? Files[0].FilePath : null;

    /// <summary>
    /// Gets the file name of the first file in this group (for sorting).
    /// </summary>
    public string FirstFileName => Files.Count > 0 ? Files[0].FileName : string.Empty;

    /// <summary>
    /// Gets the delete button label based on count.
    /// </summary>
    public string DeleteAllLabel => Count == 2 ? "🗑 Delete Both" : $"🗑 Delete All ({Count})";

    /// <summary>
    /// Gets the file extension of the first file in this group (for sorting).
    /// </summary>
    public string FileExtension => Files.Count > 0
        ? System.IO.Path.GetExtension(Files[0].FilePath).ToLowerInvariant()
        : string.Empty;

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
