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
        settings.ExcludeFolderNames.Should().BeEmpty();
        settings.FilterRules.Should().BeEmpty();
        settings.MoveTargetPath.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var rule = new FilterRule
        {
            Pattern = "*.txt",
            IsRegex = true,
            IgnoreCase = false,
            Action = FilterAction.Ignore,
            Target = FilterTarget.Filepath,
        };

        var settings = new AppSettings
        {
            TargetPaths = new List<string> { @"C:\a", @"C:\b" },
            IncludeSubdirectories = false,
            MinimumFileSize = 2048,
            ExcludeFolderNames = new List<string> { "node_modules", ".git" },
            FilterRules = new List<FilterRule> { rule },
        };

        settings.TargetPaths.Should().HaveCount(2);
        settings.IncludeSubdirectories.Should().BeFalse();
        settings.MinimumFileSize.Should().Be(2048);
        settings.ExcludeFolderNames.Should().HaveCount(2);
        settings.ExcludeFolderNames.Should().Contain("node_modules");
        settings.FilterRules.Should().HaveCount(1);
        settings.FilterRules[0].Pattern.Should().Be("*.txt");
        settings.FilterRules[0].Action.Should().Be(FilterAction.Ignore);
        settings.FilterRules[0].Target.Should().Be(FilterTarget.Filepath);
    }
}
