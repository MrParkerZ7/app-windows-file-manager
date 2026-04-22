using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class FolderSearchPatternTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var p = new FolderSearchPattern();

        p.Pattern.Should().BeEmpty();
        p.IsEnabled.Should().BeTrue();
        p.MatchType.Should().Be(FolderMatchType.Match);
        p.Priority.Should().Be(0);
    }

    [Fact]
    public void IsEnabled_WhenChanged_ShouldRaisePropertyChanged()
    {
        var p = new FolderSearchPattern();
        var changes = new List<string>();
        p.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        p.IsEnabled = false;
        p.IsEnabled = false; // no change

        changes.Should().ContainSingle(x => x == nameof(FolderSearchPattern.IsEnabled));
    }

    [Fact]
    public void MatchType_WhenChanged_ShouldRaisePropertyChanged()
    {
        var p = new FolderSearchPattern();
        var changes = new List<string>();
        p.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        p.MatchType = FolderMatchType.Include;
        p.MatchType = FolderMatchType.Include; // no change

        changes.Should().ContainSingle(x => x == nameof(FolderSearchPattern.MatchType));
    }

    [Fact]
    public void Priority_WhenChanged_ShouldRaisePropertyChanged()
    {
        var p = new FolderSearchPattern();
        var changes = new List<string>();
        p.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        p.Priority = 3;
        p.Priority = 3; // no change

        changes.Should().ContainSingle(x => x == nameof(FolderSearchPattern.Priority));
    }

    [Fact]
    public void PropertySetters_NoSubscribers_ShouldNotThrow()
    {
        var p = new FolderSearchPattern();
        var act = () =>
        {
            p.IsEnabled = false;
            p.MatchType = FolderMatchType.Include;
            p.Priority = 5;
        };
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(FolderMatchType.Include, 0)]
    [InlineData(FolderMatchType.Match, 1)]
    [InlineData(FolderMatchType.Contains, 2)]
    [InlineData(FolderMatchType.Exclude, 3)]
    [InlineData(FolderMatchType.Mismatch, 4)]
    [InlineData(FolderMatchType.NotContain, 5)]
    public void FolderMatchType_Ordinals_Preserved(FolderMatchType type, int expectedOrdinal)
    {
        ((int)type).Should().Be(expectedOrdinal);
    }
}
