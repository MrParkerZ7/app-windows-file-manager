using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class FolderSearchResultTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var r = new FolderSearchResult();

        r.FullPath.Should().BeEmpty();
        r.FolderName.Should().BeEmpty();
        r.ParentPath.Should().BeEmpty();
        r.MatchedPattern.Should().BeEmpty();
        r.IsSelected.Should().BeFalse();
        r.TotalSize.Should().Be(0);
    }

    [Fact]
    public void IsSelected_WhenChanged_ShouldRaisePropertyChanged()
    {
        var r = new FolderSearchResult();
        var changes = new List<string>();
        r.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        r.IsSelected = true;
        r.IsSelected = true; // no change

        changes.Should().ContainSingle(p => p == nameof(FolderSearchResult.IsSelected));
    }

    [Fact]
    public void TotalSize_WhenChanged_ShouldRaisePropertyChanged_ForBothTotalSizeAndDisplay()
    {
        var r = new FolderSearchResult();
        var changes = new List<string>();
        r.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        r.TotalSize = 1024;
        r.TotalSize = 1024; // no change

        changes.Should().Equal(new[]
        {
            nameof(FolderSearchResult.TotalSize),
            nameof(FolderSearchResult.TotalSizeDisplay),
        });
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(999, "999 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1_048_576, "1 MB")]
    [InlineData(1_073_741_824, "1 GB")]
    public void TotalSizeDisplay_ShouldFormat(long bytes, string expected)
    {
        new FolderSearchResult { TotalSize = bytes }.TotalSizeDisplay.Should().Be(expected);
    }

    [Fact]
    public void TotalSizeDisplay_HugeValues_ShouldCapAtPB()
    {
        var r = new FolderSearchResult { TotalSize = 1125899906842624L * 5 };
        r.TotalSizeDisplay.Should().Be("5 PB");
    }

    [Fact]
    public void TotalSizeDisplay_FractionalGB_ShouldFormatDecimals()
    {
        // 1.5 GB
        new FolderSearchResult { TotalSize = 1_610_612_736 }.TotalSizeDisplay.Should().Be("1.5 GB");
    }

    [Fact]
    public void IsSelected_NoSubscribers_ShouldNotThrow()
    {
        var r = new FolderSearchResult();
        var act = () => r.IsSelected = true;
        act.Should().NotThrow();
    }

    [Fact]
    public void TotalSize_NoSubscribers_ShouldNotThrow()
    {
        var r = new FolderSearchResult();
        var act = () => r.TotalSize = 100;
        act.Should().NotThrow();
    }
}
