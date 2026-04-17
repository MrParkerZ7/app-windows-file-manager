using FluentAssertions;
using Moq;
using WindowsFileManager.Application.Services;
using WindowsFileManager.Core.Models;
using WindowsFileManager.Core.Services;

namespace WindowsFileManager.Tests.Services;

public class SettingsServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _service = new SettingsService(_mockFileSystem.Object, @"C:\app\settings.json");
    }

    [Fact]
    public void Load_JsonDeserializesToNull_ShouldReturnDefaults()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns("null");

        var settings = _service.Load();

        settings.Should().NotBeNull();
        settings.TargetPaths.Should().BeEmpty();
    }

    [Fact]
    public void Load_FileNotExists_ShouldReturnDefaults()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(false);

        var settings = _service.Load();

        settings.TargetPaths.Should().BeEmpty();
        settings.IncludeSubdirectories.Should().BeTrue();
        settings.MinimumFileSize.Should().Be(1);
    }

    [Fact]
    public void Load_ValidJson_ShouldDeserialize()
    {
        var json = """
            {
              "TargetPaths": ["C:\\folder1", "C:\\folder2"],
              "IncludeSubdirectories": false,
              "MinimumFileSize": 1024
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.TargetPaths.Should().BeEquivalentTo(new[] { @"C:\folder1", @"C:\folder2" });
        settings.IncludeSubdirectories.Should().BeFalse();
        settings.MinimumFileSize.Should().Be(1024);
    }

    [Fact]
    public void Load_InvalidJson_ShouldReturnDefaults()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns("not json{{{");

        var settings = _service.Load();

        settings.TargetPaths.Should().BeEmpty();
        settings.IncludeSubdirectories.Should().BeTrue();
    }

    [Fact]
    public void Save_ShouldWriteJsonToFile()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);

        var settings = new AppSettings
        {
            TargetPaths = new List<string> { @"C:\test" },
            IncludeSubdirectories = false,
            MinimumFileSize = 512,
        };

        _service.Save(settings);

        _mockFileSystem.Verify(
            fs => fs.WriteAllText(
                @"C:\app\settings.json",
                It.Is<string>(s => s.Contains("C:\\\\test") && s.Contains("false"))),
            Times.Once);
    }

    [Fact]
    public void Save_DirectoryNotExists_ShouldCreateIt()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(false);

        _service.Save(new AppSettings());

        _mockFileSystem.Verify(fs => fs.CreateDirectory(@"C:\app"), Times.Once);
    }

    [Fact]
    public void Save_DirectoryExists_ShouldNotCreateIt()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);

        _service.Save(new AppSettings());

        _mockFileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SaveAndLoad_FilterRules_ShouldRoundTrip()
    {
        string? savedJson = null;
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);
        _mockFileSystem.Setup(fs => fs.WriteAllText(@"C:\app\settings.json", It.IsAny<string>()))
            .Callback<string, string>((_, json) => savedJson = json);

        var settings = new AppSettings
        {
            FilterRules = new List<FilterRule>
            {
                new() { Pattern = "*.jpg", Action = FilterAction.Include, Target = FilterTarget.Filename, IsRegex = false, IgnoreCase = true, IsEnabled = true },
                new() { Pattern = "backup", Action = FilterAction.Exclude, Target = FilterTarget.Filepath, IsRegex = true, IgnoreCase = false, IsEnabled = false },
            },
        };

        _service.Save(settings);

        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(savedJson!);

        var loaded = _service.Load();

        loaded.FilterRules.Should().HaveCount(2);
        loaded.FilterRules[0].Pattern.Should().Be("*.jpg");
        loaded.FilterRules[0].Action.Should().Be(FilterAction.Include);
        loaded.FilterRules[0].IsEnabled.Should().BeTrue();
        loaded.FilterRules[0].IgnoreCase.Should().BeTrue();
        loaded.FilterRules[1].Pattern.Should().Be("backup");
        loaded.FilterRules[1].Action.Should().Be(FilterAction.Exclude);
        loaded.FilterRules[1].Target.Should().Be(FilterTarget.Filepath);
        loaded.FilterRules[1].IsRegex.Should().BeTrue();
        loaded.FilterRules[1].IgnoreCase.Should().BeFalse();
        loaded.FilterRules[1].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SaveAndLoad_ExcludeFolderNames_ShouldRoundTrip()
    {
        string? savedJson = null;
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);
        _mockFileSystem.Setup(fs => fs.WriteAllText(@"C:\app\settings.json", It.IsAny<string>()))
            .Callback<string, string>((_, json) => savedJson = json);

        var settings = new AppSettings
        {
            ExcludeFolderNames = new List<string> { "node_modules", ".git", "bin" },
        };

        _service.Save(settings);

        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(savedJson!);

        var loaded = _service.Load();

        loaded.ExcludeFolderNames.Should().BeEquivalentTo(new[] { "node_modules", ".git", "bin" });
    }

    [Fact]
    public void SaveAndLoad_WindowState_ShouldRoundTrip()
    {
        string? savedJson = null;
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);
        _mockFileSystem.Setup(fs => fs.WriteAllText(@"C:\app\settings.json", It.IsAny<string>()))
            .Callback<string, string>((_, json) => savedJson = json);

        var settings = new AppSettings
        {
            WindowLeft = 100.5,
            WindowTop = 200.0,
            WindowWidth = 1400.0,
            WindowHeight = 900.0,
            IsMaximized = true,
        };

        _service.Save(settings);

        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(savedJson!);

        var loaded = _service.Load();

        loaded.WindowLeft.Should().Be(100.5);
        loaded.WindowTop.Should().Be(200.0);
        loaded.WindowWidth.Should().Be(1400.0);
        loaded.WindowHeight.Should().Be(900.0);
        loaded.IsMaximized.Should().BeTrue();
    }

    [Fact]
    public void Load_OldSettingsWithDisplaySummary_ShouldDeserialize()
    {
        var json = """
            {
              "TargetPaths": ["C:\\folder1"],
              "FilterRules": [
                {
                  "Pattern": "test",
                  "IsRegex": false,
                  "IgnoreCase": true,
                  "Action": 0,
                  "Target": 0,
                  "DisplaySummary": "Select | Filename | \"test\" [IgnoreCase]"
                }
              ]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.FilterRules.Should().HaveCount(1);
        settings.FilterRules[0].Pattern.Should().Be("test");
        settings.FilterRules[0].Action.Should().Be(FilterAction.Include);
        settings.FilterRules[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Load_OldSettingsWithoutIsEnabled_ShouldDefaultToTrue()
    {
        var json = """
            {
              "FilterRules": [
                {
                  "Pattern": "*.png",
                  "IsRegex": false,
                  "IgnoreCase": true,
                  "Action": 1,
                  "Target": 0
                }
              ]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.FilterRules.Should().HaveCount(1);
        settings.FilterRules[0].IsEnabled.Should().BeTrue();
        settings.FilterRules[0].Action.Should().Be(FilterAction.Exclude);
    }

    [Fact]
    public void Load_OldSettingsWithoutWindowState_ShouldDefaultToNull()
    {
        var json = """
            {
              "TargetPaths": ["C:\\folder1"]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.WindowLeft.Should().BeNull();
        settings.WindowTop.Should().BeNull();
        settings.WindowWidth.Should().BeNull();
        settings.WindowHeight.Should().BeNull();
        settings.IsMaximized.Should().BeFalse();
    }
}
