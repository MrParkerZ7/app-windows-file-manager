using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var settings = new AppSettings();

        settings.Profiles.Should().BeEmpty();
        settings.ActiveProfileName.Should().Be("Default");
        settings.ActionHistory.Should().BeEmpty();
        settings.WindowLeft.Should().BeNull();
        settings.WindowTop.Should().BeNull();
        settings.WindowWidth.Should().BeNull();
        settings.WindowHeight.Should().BeNull();
        settings.IsMaximized.Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var profile = new ProfileSettings { Name = "Work" };
        var settings = new AppSettings
        {
            Profiles = new List<ProfileSettings> { profile },
            ActiveProfileName = "Work",
            ActionHistory = new List<ActionHistoryEntry>
            {
                new() { Kind = ActionHistoryKind.RecycleFiles, Summary = "Recycled 3 files" },
            },
            WindowLeft = 120,
            WindowTop = 80,
            WindowWidth = 1200,
            WindowHeight = 800,
            IsMaximized = true,
        };

        settings.Profiles.Should().ContainSingle();
        settings.Profiles[0].Name.Should().Be("Work");
        settings.ActiveProfileName.Should().Be("Work");
        settings.ActionHistory.Should().ContainSingle();
        settings.ActionHistory[0].Summary.Should().Be("Recycled 3 files");
        settings.WindowLeft.Should().Be(120);
        settings.WindowTop.Should().Be(80);
        settings.WindowWidth.Should().Be(1200);
        settings.WindowHeight.Should().Be(800);
        settings.IsMaximized.Should().BeTrue();
    }
}
