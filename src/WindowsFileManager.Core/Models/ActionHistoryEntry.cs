using System;
using System.Collections.Generic;

namespace WindowsFileManager.Core.Models;

/// <summary>
/// Kind of reversible action recorded in the undo history.
/// </summary>
public enum ActionHistoryKind
{
    /// <summary>Files moved from source to destination (reversible via move-back).</summary>
    MoveFiles,

    /// <summary>Files sent to Recycle Bin (reversible via Shell Restore).</summary>
    RecycleFiles,

    /// <summary>Directories sent to Recycle Bin (reversible via Shell Restore).</summary>
    RecycleDirectories,
}

/// <summary>A (source → destination) pair for a move that can be reversed.</summary>
public class ActionHistoryMove
{
    /// <summary>Gets or sets the original source path.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination path (where the file currently is).</summary>
    public string Destination { get; set; } = string.Empty;
}

/// <summary>
/// A single reversible action in the undo history stack. Represents one user operation
/// that may have affected many files or folders.
/// </summary>
public class ActionHistoryEntry
{
    /// <summary>Gets or sets the kind of action.</summary>
    public ActionHistoryKind Kind { get; set; }

    /// <summary>Gets or sets the (source → destination) pairs for <see cref="ActionHistoryKind.MoveFiles"/>.</summary>
    public List<ActionHistoryMove> Moves { get; set; } = new();

    /// <summary>Gets or sets the original paths sent to Recycle Bin (for recycle kinds).</summary>
    public List<string> RecycledPaths { get; set; } = new();

    /// <summary>Gets or sets the human-readable summary, e.g., "Recycled 47 .log files".</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets when the action occurred (local time).</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Gets the item count in this action.</summary>
    public int ItemCount => Kind == ActionHistoryKind.MoveFiles ? Moves.Count : RecycledPaths.Count;
}
