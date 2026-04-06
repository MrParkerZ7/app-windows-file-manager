using FluentAssertions;
using WindowsFileManager.Core.Models;

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
        settings.FilenameFilterText.Should().BeEmpty();
        settings.IsFilenameRegex.Should().BeFalse();
        settings.IsFilenameIgnoreCase.Should().BeTrue();
        settings.FilepathFilterText.Should().BeEmpty();
        settings.IsFilepathRegex.Should().BeFalse();
        settings.IsFilepathIgnoreCase.Should().BeTrue();
        settings.IgnoreFilenameFilterText.Should().BeEmpty();
        settings.IsIgnoreFilenameRegex.Should().BeFalse();
        settings.IsIgnoreFilenameIgnoreCase.Should().BeTrue();
        settings.IgnoreFilepathFilterText.Should().BeEmpty();
        settings.IsIgnoreFilepathRegex.Should().BeFalse();
        settings.IsIgnoreFilepathIgnoreCase.Should().BeTrue();
        settings.IsContainSectionVisible.Should().BeTrue();
        settings.IsIgnoreSectionVisible.Should().BeTrue();
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var settings = new AppSettings
        {
            TargetPaths = new List<string> { @"C:\a", @"C:\b" },
            IncludeSubdirectories = false,
            MinimumFileSize = 2048,
            FilenameFilterText = "*.txt",
            IsFilenameRegex = true,
            IsFilenameIgnoreCase = false,
            FilepathFilterText = @"C:\temp",
            IsFilepathRegex = true,
            IsFilepathIgnoreCase = false,
            IgnoreFilenameFilterText = "thumbs",
            IsIgnoreFilenameRegex = true,
            IsIgnoreFilenameIgnoreCase = false,
            IgnoreFilepathFilterText = @"C:\cache",
            IsIgnoreFilepathRegex = true,
            IsIgnoreFilepathIgnoreCase = false,
            IsContainSectionVisible = false,
            IsIgnoreSectionVisible = false,
        };

        settings.TargetPaths.Should().HaveCount(2);
        settings.IncludeSubdirectories.Should().BeFalse();
        settings.MinimumFileSize.Should().Be(2048);
        settings.FilenameFilterText.Should().Be("*.txt");
        settings.IsFilenameRegex.Should().BeTrue();
        settings.IsFilenameIgnoreCase.Should().BeFalse();
        settings.FilepathFilterText.Should().Be(@"C:\temp");
        settings.IsFilepathRegex.Should().BeTrue();
        settings.IsFilepathIgnoreCase.Should().BeFalse();
        settings.IgnoreFilenameFilterText.Should().Be("thumbs");
        settings.IsIgnoreFilenameRegex.Should().BeTrue();
        settings.IsIgnoreFilenameIgnoreCase.Should().BeFalse();
        settings.IgnoreFilepathFilterText.Should().Be(@"C:\cache");
        settings.IsIgnoreFilepathRegex.Should().BeTrue();
        settings.IsIgnoreFilepathIgnoreCase.Should().BeFalse();
        settings.IsContainSectionVisible.Should().BeFalse();
        settings.IsIgnoreSectionVisible.Should().BeFalse();
    }
}
