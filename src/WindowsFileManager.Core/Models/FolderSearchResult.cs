using System.ComponentModel;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a folder found during folder search.
/// </summary>
public class FolderSearchResult : INotifyPropertyChanged
{
    private bool _isSelected;

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
}
