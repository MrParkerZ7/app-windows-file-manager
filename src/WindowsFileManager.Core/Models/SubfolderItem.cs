using System.Collections.Generic;
using System.ComponentModel;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a subfolder name found across search results, with selection and occurrence count.
/// </summary>
public class SubfolderItem : INotifyPropertyChanged
{
    private bool _isSelected;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the subfolder name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how many result folders contain this subfolder.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the full paths where this subfolder was found.
    /// </summary>
    public List<SubfolderLocation> Locations { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this subfolder is selected for clearing.
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
    /// Gets the display text combining name and count.
    /// </summary>
    public string Display => $"{Name} ({Count})";
}

/// <summary>
/// Represents a specific location where a subfolder was found.
/// </summary>
public class SubfolderLocation
{
    /// <summary>
    /// Gets or sets the parent folder path (the search result folder).
    /// </summary>
    public string ParentPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path of the subfolder.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
}
