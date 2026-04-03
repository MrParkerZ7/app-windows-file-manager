using FluentAssertions;
using WindowsFileManager.Models;

namespace WindowsFileManager.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var settings = new AppSettings();

        settings.TargetPaths.Should().BeEmpty();
        settings.IncludeSubdirectories.Should().BeTrue();
        settings.MinimumFileSize.Should().Be(1);
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var settings = new AppSettings
        {
            TargetPaths = new List<string> { @"C:\a", @"C:\b" },
            IncludeSubdirectories = false,
            MinimumFileSize = 2048,
        };

        settings.TargetPaths.Should().HaveCount(2);
        settings.IncludeSubdirectories.Should().BeFalse();
        settings.MinimumFileSize.Should().Be(2048);
    }
}
