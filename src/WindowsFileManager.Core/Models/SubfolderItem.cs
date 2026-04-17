using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// Represents a subfolder name found across search results, with selection and occurrence count.
/// </summary>
public class SubfolderItem : INotifyPropertyChanged
{
    /// <summary>Page size used for paging the Locations list in the UI.</summary>
    public const int PageSize = 50;

    private bool _isSelected;
    private string _locationFilter = string.Empty;
    private int _currentPage;

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
    /// Gets or sets the total size in bytes across all locations (files: own size, subfolders: recursive size).
    /// </summary>
    public long TotalSize { get; set; }

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
                Raise(nameof(IsSelected));
            }
        }
    }

    /// <summary>Gets or sets the filter text for the Locations list (case-insensitive substring match).</summary>
    public string LocationFilter
    {
        get => _locationFilter;
        set
        {
            var v = value ?? string.Empty;
            if (_locationFilter != v)
            {
                _locationFilter = v;
                _currentPage = 0;
                RaisePaging();
            }
        }
    }

    /// <summary>Gets the 0-based current page index.</summary>
    public int CurrentPage => _currentPage;

    /// <summary>Gets the total page count.</summary>
    public int TotalPages
    {
        get
        {
            var count = FilteredCount;
            return count == 0 ? 1 : (int)Math.Ceiling(count / (double)PageSize);
        }
    }

    /// <summary>Gets the filtered locations count (before paging).</summary>
    public int FilteredCount
    {
        get
        {
            if (string.IsNullOrEmpty(_locationFilter))
            {
                return Locations.Count;
            }

            return Locations.Count(l =>
                l.FullPath.Contains(_locationFilter, StringComparison.OrdinalIgnoreCase) ||
                l.ParentPath.Contains(_locationFilter, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Gets the paged + filtered locations for the current page.</summary>
    public IEnumerable<SubfolderLocation> PagedLocations
    {
        get
        {
            IEnumerable<SubfolderLocation> source = Locations;
            if (!string.IsNullOrEmpty(_locationFilter))
            {
                source = source.Where(l =>
                    l.FullPath.Contains(_locationFilter, StringComparison.OrdinalIgnoreCase) ||
                    l.ParentPath.Contains(_locationFilter, StringComparison.OrdinalIgnoreCase));
            }

            return source.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        }
    }

    /// <summary>Gets a human-readable page status, e.g., "Page 1 of 5 · 237 results".</summary>
    public string PageStatus
    {
        get
        {
            var filtered = FilteredCount;
            if (filtered == 0)
            {
                return "No matches";
            }

            var pages = TotalPages;
            return $"Page {_currentPage + 1} of {pages} · {filtered} result{(filtered == 1 ? string.Empty : "s")}";
        }
    }

    /// <summary>Gets a value indicating whether a next page is available.</summary>
    public bool CanGoNextPage => _currentPage + 1 < TotalPages;

    /// <summary>Gets a value indicating whether a previous page is available.</summary>
    public bool CanGoPrevPage => _currentPage > 0;

    /// <summary>
    /// Gets the display text combining name and count.
    /// </summary>
    public string Display => $"{Name} ({Count})";

    /// <summary>Advance to the next page (no-op if none).</summary>
    public void NextPage()
    {
        if (CanGoNextPage)
        {
            _currentPage++;
            RaisePaging();
        }
    }

    /// <summary>Go to the previous page (no-op if none).</summary>
    public void PrevPage()
    {
        if (CanGoPrevPage)
        {
            _currentPage--;
            RaisePaging();
        }
    }

    private void Raise(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void RaisePaging()
    {
        Raise(nameof(CurrentPage));
        Raise(nameof(TotalPages));
        Raise(nameof(FilteredCount));
        Raise(nameof(PagedLocations));
        Raise(nameof(PageStatus));
        Raise(nameof(CanGoNextPage));
        Raise(nameof(CanGoPrevPage));
    }
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
