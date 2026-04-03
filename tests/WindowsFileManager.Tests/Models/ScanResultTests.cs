using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class ScanResultTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var result = new ScanResult();

        result.TotalFilesScanned.Should().Be(0);
        result.TotalDuplicates.Should().Be(0);
        result.TotalWastedBytes.Should().Be(0);
        result.DuplicateGroups.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void FormattedWastedSize_ShouldFormatCorrectly()
    {
        var result = new ScanResult { TotalWastedBytes = 5242880 }; // 5 MB

        result.FormattedWastedSize.Should().Be("5.0 MB");
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var groups = new List<DuplicateGroup> { new() { Hash = "abc" } };
        var duration = TimeSpan.FromSeconds(3.5);

        var result = new ScanResult
        {
            TotalFilesScanned = 100,
            TotalDuplicates = 10,
            TotalWastedBytes = 2048,
            DuplicateGroups = groups,
            Duration = duration,
        };

        result.TotalFilesScanned.Should().Be(100);
        result.TotalDuplicates.Should().Be(10);
        result.TotalWastedBytes.Should().Be(2048);
        result.DuplicateGroups.Should().HaveCount(1);
        result.Duration.Should().Be(duration);
    }
}
