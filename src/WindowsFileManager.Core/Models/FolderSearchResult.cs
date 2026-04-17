using System.ComponentModel;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a folder found during folder search.
/// </summary>
public class FolderSearchResult : INotifyPropertyChanged
{
    private bool _isSelected;
    private long _totalSize;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets a value indicating whether this result is selected for action.
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

    /// <summary>
    /// Gets or sets the total size in bytes (recursive file size sum).
    /// </summary>
    public long TotalSize
    {
        get => _totalSize;
        set
        {
            if (_totalSize != value)
            {
                _totalSize = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSize)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSizeDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets a human-readable display of <see cref="TotalSize"/> (e.g., "123 MB").
    /// </summary>
    public string TotalSizeDisplay
    {
        get
        {
            var bytes = TotalSize;
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            double value = bytes;
            string[] suffixes = { "KB", "MB", "GB", "TB", "PB" };
            var i = -1;
            do
            {
                value /= 1024;
                i++;
            }
            while (value >= 1024 && i < suffixes.Length - 1);

            return $"{value:0.##} {suffixes[i]}";
        }
    }
}
