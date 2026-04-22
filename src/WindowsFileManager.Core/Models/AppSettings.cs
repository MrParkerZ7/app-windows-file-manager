namespace WindowsFileManager.Core.Models;

/// <summary>
/// Persisted application settings. Per-tab workflow state lives inside <see cref="ProfileSettings"/>.
/// Window geometry and the action history log are global (shared across all profiles).
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the list of saved profiles. At least one profile is always present.
    /// </summary>
    public List<ProfileSettings> Profiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the name of the currently active profile.
    /// </summary>
    public string ActiveProfileName { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the persisted transaction history (most recent first) for undo-across-sessions.
    /// </summary>
    public List<ActionHistoryEntry> ActionHistory { get; set; } = new();

    /// <summary>
    /// Gets or sets the window left position.
    /// </summary>
    public double? WindowLeft { get; set; }

    /// <summary>
    /// Gets or sets the window top position.
    /// </summary>
    public double? WindowTop { get; set; }

    /// <summary>
    /// Gets or sets the window width.
    /// </summary>
    public double? WindowWidth { get; set; }

    /// <summary>
    /// Gets or sets the window height.
    /// </summary>
    public double? WindowHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the window was maximized.
    /// </summary>
    public bool IsMaximized { get; set; }
}
