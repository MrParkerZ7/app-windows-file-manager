using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class ProfileSettingsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var profile = new ProfileSettings();

        profile.Name.Should().Be("Default");
        profile.TargetPaths.Should().BeEmpty();
        profile.IncludeSubdirectories.Should().BeTrue();
        profile.MinimumFileSize.Should().Be(1);
        profile.IsMiniPreview.Should().BeTrue();
        profile.IsAutoPreview.Should().BeTrue();
        profile.IsAutoPlay.Should().BeFalse();
        profile.SelectedSortOption.Should().Be("Size (largest)");
        profile.Volume.Should().Be(0.5);
        profile.MoveTargetPath.Should().BeEmpty();
        profile.ExcludeFolderNames.Should().BeEmpty();
        profile.DisabledTargetPaths.Should().BeEmpty();
        profile.DisabledExcludeFolderNames.Should().BeEmpty();
        profile.FilterRules.Should().BeEmpty();
        profile.FolderSearchPatterns.Should().BeEmpty();
        profile.FolderSearchMaxDepth.Should().BeNull();
        profile.FolderSearchResultPaths.Should().BeEmpty();
        profile.SelectedFolderSearchResultPaths.Should().BeEmpty();
        profile.LinkSiblingsLayer.Should().Be(1);
        profile.LinkSiblingsPrefix.Should().BeEmpty();
        profile.DuplicateMatchByRegex.Should().BeFalse();
        profile.DuplicateMatchRegex.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var rule = new FilterRule { Pattern = "*.txt", Action = FilterAction.Exclude };
        var pattern = new FolderSearchPattern { Pattern = "src", MatchType = FolderMatchType.Include };

        var profile = new ProfileSettings
        {
            Name = "Projects",
            TargetPaths = new List<string> { @"C:\a", @"C:\b" },
            IncludeSubdirectories = false,
            MinimumFileSize = 2048,
            IsMiniPreview = false,
            IsAutoPreview = false,
            IsAutoPlay = true,
            SelectedSortOption = "Name (A-Z)",
            Volume = 0.25,
            MoveTargetPath = @"D:\sorted",
            ExcludeFolderNames = new List<string> { "node_modules" },
            DisabledTargetPaths = new List<string> { @"C:\b" },
            DisabledExcludeFolderNames = new List<string> { "node_modules" },
            FilterRules = new List<FilterRule> { rule },
            FolderSearchPatterns = new List<FolderSearchPattern> { pattern },
            FolderSearchMaxDepth = 3,
            FolderSearchResultPaths = new List<string> { @"C:\a\src" },
            SelectedFolderSearchResultPaths = new List<string> { @"C:\a\src" },
            LinkSiblingsLayer = 3,
            LinkSiblingsPrefix = "link-",
            DuplicateMatchByRegex = true,
            DuplicateMatchRegex = "(?i)(abc).*?(\\d{8})",
        };

        profile.Name.Should().Be("Projects");
        profile.TargetPaths.Should().HaveCount(2);
        profile.IncludeSubdirectories.Should().BeFalse();
        profile.MinimumFileSize.Should().Be(2048);
        profile.IsMiniPreview.Should().BeFalse();
        profile.IsAutoPreview.Should().BeFalse();
        profile.IsAutoPlay.Should().BeTrue();
        profile.SelectedSortOption.Should().Be("Name (A-Z)");
        profile.Volume.Should().Be(0.25);
        profile.MoveTargetPath.Should().Be(@"D:\sorted");
        profile.ExcludeFolderNames.Should().ContainSingle();
        profile.DisabledTargetPaths.Should().ContainSingle();
        profile.DisabledExcludeFolderNames.Should().ContainSingle();
        profile.FilterRules.Should().ContainSingle().Which.Pattern.Should().Be("*.txt");
        profile.FolderSearchPatterns.Should().ContainSingle().Which.Pattern.Should().Be("src");
        profile.FolderSearchMaxDepth.Should().Be(3);
        profile.FolderSearchResultPaths.Should().ContainSingle();
        profile.SelectedFolderSearchResultPaths.Should().ContainSingle();
        profile.LinkSiblingsLayer.Should().Be(3);
        profile.LinkSiblingsPrefix.Should().Be("link-");
        profile.DuplicateMatchByRegex.Should().BeTrue();
        profile.DuplicateMatchRegex.Should().Be("(?i)(abc).*?(\\d{8})");
    }
}
