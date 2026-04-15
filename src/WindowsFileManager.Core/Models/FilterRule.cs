namespace WindowsFileManager.Core.Models;

/// <summary>
/// Defines the action a filter rule performs.
/// </summary>
public enum FilterAction
{
    /// <summary>Contains (check) files matching the pattern.</summary>
    Contains,

    /// <summary>Ignore (uncheck) files matching the pattern.</summary>
    Ignore,
}

/// <summary>
/// Defines what part of the file path to match against.
/// </summary>
public enum FilterTarget
{
    /// <summary>Match against filename only.</summary>
    Filename,

    /// <summary>Match against full file path.</summary>
    Filepath,
}

/// <summary>
/// A single dynamic filter rule for selecting or ignoring files.
/// </summary>
public class FilterRule : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isEnabled = true;

    /// <inheritdoc/>
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the priority (1 = highest). Display only, not serialized for ordering.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the text pattern to match.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this rule is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use regex matching.
    /// </summary>
    public bool IsRegex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore case.
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets the filter action (Select or Ignore).
    /// </summary>
    public FilterAction Action { get; set; } = FilterAction.Contains;

    /// <summary>
    /// Gets or sets the filter target (Filename or Filepath).
    /// </summary>
    public FilterTarget Target { get; set; } = FilterTarget.Filename;

    /// <summary>
    /// Gets a display summary of the rule.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplaySummary
    {
        get
        {
            var flags = new List<string>();
            if (IsRegex)
            {
                flags.Add("Regex");
            }

            if (IgnoreCase)
            {
                flags.Add("IgnoreCase");
            }

            var flagText = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : string.Empty;
            return $"{Action} | {Target} | \"{Pattern}\"{flagText}";
        }
    }
}
