using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class ScanOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var options = new ScanOptions();

        options.TargetPaths.Should().BeEmpty();
        options.IncludeSubdirectories.Should().BeTrue();
        options.MinimumFileSize.Should().Be(1);
        options.FileExtensions.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var options = new ScanOptions
        {
            TargetPaths = new List<string> { @"C:\test", @"D:\data" },
            IncludeSubdirectories = false,
            MinimumFileSize = 1024,
            FileExtensions = new List<string> { "txt", "pdf" },
        };

        options.TargetPaths.Should().BeEquivalentTo(new[] { @"C:\test", @"D:\data" });
        options.IncludeSubdirectories.Should().BeFalse();
        options.MinimumFileSize.Should().Be(1024);
        options.FileExtensions.Should().BeEquivalentTo(new[] { "txt", "pdf" });
    }
}
