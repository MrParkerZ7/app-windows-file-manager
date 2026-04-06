using System.ComponentModel;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a file discovered during scanning.
/// </summary>
public class ScannedFile : INotifyPropertyChanged
{
    private bool _isFileSelected;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets a value indicating whether this file is selected for bulk actions.
    /// </summary>
    public bool IsFileSelected
    {
        get => _isFileSelected;
        set
        {
            if (_isFileSelected != value)
            {
                _isFileSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFileSelected)));
            }
        }
    }

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
    /// <param name="bytes">The number of bytes to format.</param>
    /// <returns>A human-readable size string (e.g., "1.23 MB").</returns>
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
