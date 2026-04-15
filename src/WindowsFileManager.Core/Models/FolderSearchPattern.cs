using System.ComponentModel;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// Defines how a folder search pattern matches folder names.
/// </summary>
public enum FolderMatchType
{
    /// <summary>Folder name contains the pattern text.</summary>
    Contains,

    /// <summary>Folder name exactly matches the pattern text.</summary>
    Match,
}

/// <summary>
/// A folder name search pattern with match type and enable/disable toggle.
/// </summary>
public class FolderSearchPattern : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private FolderMatchType _matchType = FolderMatchType.Match;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the pattern text to search for.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this pattern is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the match type (Contains or Match).
    /// </summary>
    public FolderMatchType MatchType
    {
        get => _matchType;
        set
        {
            if (_matchType != value)
            {
                _matchType = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchType)));
            }
        }
    }
}
