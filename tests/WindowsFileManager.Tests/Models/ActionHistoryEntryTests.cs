using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class ActionHistoryEntryTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var entry = new ActionHistoryEntry();

        entry.Kind.Should().Be(ActionHistoryKind.MoveFiles);
        entry.Moves.Should().BeEmpty();
        entry.RecycledPaths.Should().BeEmpty();
        entry.Summary.Should().BeEmpty();
        entry.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        entry.ItemCount.Should().Be(0);
    }

    [Fact]
    public void ItemCount_MoveFiles_ShouldReturnMovesCount()
    {
        var entry = new ActionHistoryEntry
        {
            Kind = ActionHistoryKind.MoveFiles,
            Moves = new List<(string Source, string Destination)>
            {
                (@"C:\a\1.txt", @"C:\b\1.txt"),
                (@"C:\a\2.txt", @"C:\b\2.txt"),
                (@"C:\a\3.txt", @"C:\b\3.txt"),
            },
            RecycledPaths = new List<string> { "unused" },
        };

        entry.ItemCount.Should().Be(3);
    }

    [Fact]
    public void ItemCount_RecycleFiles_ShouldReturnRecycledPathsCount()
    {
        var entry = new ActionHistoryEntry
        {
            Kind = ActionHistoryKind.RecycleFiles,
            Moves = new List<(string Source, string Destination)> { (@"C:\a", @"C:\b") },
            RecycledPaths = new List<string> { @"C:\x\1.log", @"C:\x\2.log" },
        };

        entry.ItemCount.Should().Be(2);
    }

    [Fact]
    public void ItemCount_RecycleDirectories_ShouldReturnRecycledPathsCount()
    {
        var entry = new ActionHistoryEntry
        {
            Kind = ActionHistoryKind.RecycleDirectories,
            RecycledPaths = new List<string> { @"C:\a\bin", @"C:\b\bin", @"C:\c\bin", @"C:\d\bin" },
        };

        entry.ItemCount.Should().Be(4);
    }

    [Fact]
    public void Properties_ShouldRoundTrip()
    {
        var ts = new DateTime(2026, 4, 18, 10, 30, 0);
        var entry = new ActionHistoryEntry
        {
            Kind = ActionHistoryKind.RecycleFiles,
            RecycledPaths = new List<string> { @"C:\a\b.log" },
            Summary = "Recycled 1 file",
            Timestamp = ts,
        };

        entry.Kind.Should().Be(ActionHistoryKind.RecycleFiles);
        entry.RecycledPaths.Should().ContainSingle(p => p == @"C:\a\b.log");
        entry.Summary.Should().Be("Recycled 1 file");
        entry.Timestamp.Should().Be(ts);
    }
}
