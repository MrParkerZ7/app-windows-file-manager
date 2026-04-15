using System.Diagnostics.CodeAnalysis;

namespace WindowsFileManager.ViewModels;

/// <summary>
/// A string item with an enable/disable toggle for temporary exclusion.
/// </summary>
[ExcludeFromCodeCoverage]
public class ToggleItem : ViewModelBase
{
    private bool _isEnabled = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToggleItem"/> class.
    /// </summary>
    /// <param name="value">The string value.</param>
    public ToggleItem(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}
