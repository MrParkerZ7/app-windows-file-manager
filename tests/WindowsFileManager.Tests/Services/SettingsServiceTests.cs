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
    public void Load_FileNotExists_ShouldReturnDefaultsWithDefaultProfile()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(false);

        var settings = _service.Load();

        settings.Profiles.Should().ContainSingle();
        settings.Profiles[0].Name.Should().Be("Default");
        settings.ActiveProfileName.Should().Be("Default");
    }

    [Fact]
    public void Load_JsonDeserializesToNull_ShouldReturnDefaults()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns("null");

        var settings = _service.Load();

        settings.Profiles.Should().ContainSingle();
        settings.Profiles[0].Name.Should().Be("Default");
    }

    [Fact]
    public void Load_InvalidJson_ShouldReturnDefaults()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns("not json{{{");

        var settings = _service.Load();

        settings.Profiles.Should().ContainSingle();
        settings.ActiveProfileName.Should().Be("Default");
    }

    [Fact]
    public void Load_ValidNewFormat_ShouldDeserializeProfiles()
    {
        var json = """
            {
              "Profiles": [
                { "Name": "Work", "TargetPaths": ["C:\\projects"], "IncludeSubdirectories": false },
                { "Name": "Photos", "TargetPaths": ["D:\\photos"] }
              ],
              "ActiveProfileName": "Photos"
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles.Should().HaveCount(2);
        settings.Profiles[0].Name.Should().Be("Work");
        settings.Profiles[0].TargetPaths.Should().ContainSingle().Which.Should().Be(@"C:\projects");
        settings.Profiles[0].IncludeSubdirectories.Should().BeFalse();
        settings.Profiles[1].Name.Should().Be("Photos");
        settings.ActiveProfileName.Should().Be("Photos");
    }

    [Fact]
    public void Load_MissingActiveProfile_ShouldFallBackToFirst()
    {
        var json = """
            {
              "Profiles": [
                { "Name": "A" },
                { "Name": "B" }
              ],
              "ActiveProfileName": "NoSuchProfile"
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.ActiveProfileName.Should().Be("A");
    }

    [Fact]
    public void Load_EmptyActiveProfileName_ShouldFallBackToFirst()
    {
        var json = """
            {
              "Profiles": [
                { "Name": "Alpha" }
              ],
              "ActiveProfileName": ""
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.ActiveProfileName.Should().Be("Alpha");
    }

    [Fact]
    public void Load_LegacyFlatJson_ShouldMigrateIntoDefaultProfile()
    {
        var json = """
            {
              "TargetPaths": ["C:\\folder1", "C:\\folder2"],
              "DisabledTargetPaths": ["C:\\folder2"],
              "ExcludeFolderNames": ["node_modules", ".git"],
              "DisabledExcludeFolderNames": [".git"],
              "FolderSearchResultPaths": ["C:\\folder1\\src"],
              "SelectedFolderSearchResultPaths": ["C:\\folder1\\src"],
              "IncludeSubdirectories": false,
              "IsMiniPreview": false,
              "IsAutoPreview": false,
              "IsAutoPlay": true,
              "MinimumFileSize": 2048,
              "Volume": 0.25,
              "SelectedSortOption": "Name (A-Z)",
              "MoveTargetPath": "D:\\sorted",
              "FilterRules": [
                { "Pattern": "*.jpg", "Action": 0, "Target": 0, "IsRegex": false, "IgnoreCase": true, "IsEnabled": true }
              ],
              "FolderSearchPatterns": [
                { "Pattern": "src", "MatchType": 0, "IsEnabled": true }
              ]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles.Should().ContainSingle();
        var profile = settings.Profiles[0];
        profile.Name.Should().Be("Default");
        profile.TargetPaths.Should().BeEquivalentTo(new[] { @"C:\folder1", @"C:\folder2" });
        profile.DisabledTargetPaths.Should().ContainSingle().Which.Should().Be(@"C:\folder2");
        profile.ExcludeFolderNames.Should().BeEquivalentTo(new[] { "node_modules", ".git" });
        profile.DisabledExcludeFolderNames.Should().ContainSingle().Which.Should().Be(".git");
        profile.FolderSearchResultPaths.Should().ContainSingle();
        profile.SelectedFolderSearchResultPaths.Should().ContainSingle();
        profile.IncludeSubdirectories.Should().BeFalse();
        profile.IsMiniPreview.Should().BeFalse();
        profile.IsAutoPreview.Should().BeFalse();
        profile.IsAutoPlay.Should().BeTrue();
        profile.MinimumFileSize.Should().Be(2048);
        profile.Volume.Should().Be(0.25);
        profile.SelectedSortOption.Should().Be("Name (A-Z)");
        profile.MoveTargetPath.Should().Be(@"D:\sorted");
        profile.FilterRules.Should().ContainSingle().Which.Pattern.Should().Be("*.jpg");
        profile.FolderSearchPatterns.Should().ContainSingle().Which.Pattern.Should().Be("src");
        settings.ActiveProfileName.Should().Be("Default");
    }

    [Fact]
    public void Load_LegacyMalformedNestedFilter_ShouldStillMigrateOtherFields()
    {
        // FilterRules contains one valid and one broken entry — broken should skip, valid should appear.
        var json = """
            {
              "TargetPaths": ["C:\\ok"],
              "FilterRules": [
                { "Pattern": "*.ok", "Action": 0, "Target": 0, "IsRegex": false, "IgnoreCase": true, "IsEnabled": true }
              ]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles.Should().ContainSingle();
        settings.Profiles[0].TargetPaths.Should().ContainSingle().Which.Should().Be(@"C:\ok");
        settings.Profiles[0].FilterRules.Should().ContainSingle();
    }

    [Fact]
    public void Load_LegacyNonArrayFields_ShouldIgnoreThem()
    {
        var json = """
            {
              "TargetPaths": "not-an-array",
              "FilterRules": "also-not-an-array",
              "IncludeSubdirectories": "not-a-bool"
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        var profile = settings.Profiles.Should().ContainSingle().Subject;
        profile.TargetPaths.Should().BeEmpty();
        profile.FilterRules.Should().BeEmpty();
        profile.IncludeSubdirectories.Should().BeTrue();
    }

    [Fact]
    public void Load_LegacyFilterRulesWithTypeMismatch_ShouldSwallowJsonException()
    {
        // Action expects a number; passing an object triggers JsonException deep in nested deserialization.
        // The migration code must catch it and still return a (partially populated) profile.
        var json = """
            {
              "TargetPaths": ["C:\\still-here"],
              "FilterRules": [
                { "Pattern": "oops", "Action": { "nested": "object" }, "Target": 0, "IsRegex": false, "IgnoreCase": true, "IsEnabled": true }
              ]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles.Should().ContainSingle();
        settings.Profiles[0].Name.Should().Be("Default");
    }

    [Fact]
    public void Load_LegacyArrayAtRoot_ShouldYieldEmptyDefaultProfile()
    {
        // JSON is a valid JSON array, not an object — migration must cope by falling back to defaults.
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns("[]");

        var settings = _service.Load();

        var profile = settings.Profiles.Should().ContainSingle().Subject;
        profile.Name.Should().Be("Default");
        profile.TargetPaths.Should().BeEmpty();
    }

    [Fact]
    public void Load_LegacyStringListWithNullEntry_ShouldSkipNull()
    {
        // When a string list contains a null element, migration should skip it without throwing.
        var json = """
            {
              "TargetPaths": ["C:\\real", null, "C:\\other"]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles[0].TargetPaths.Should().BeEquivalentTo(new[] { @"C:\real", @"C:\other" });
    }

    [Fact]
    public void Load_LegacyObjectListWithNullEntry_ShouldSkipNull()
    {
        var json = """
            {
              "FilterRules": [
                { "Pattern": "ok", "Action": 0, "Target": 0, "IsRegex": false, "IgnoreCase": true, "IsEnabled": true },
                null
              ]
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles[0].FilterRules.Should().ContainSingle();
    }

    [Fact]
    public void Load_LegacyBoolean_ShouldRespectFalseExplicitly()
    {
        var json = """
            {
              "IsAutoPreview": false,
              "IsMiniPreview": true
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles[0].IsAutoPreview.Should().BeFalse();
        settings.Profiles[0].IsMiniPreview.Should().BeTrue();
    }

    [Fact]
    public void Load_LegacyNumericOverflow_ShouldFallBack()
    {
        // MinimumFileSize > Int64 max — TryGetInt64 fails, falls back to default.
        var json = """
            {
              "MinimumFileSize": 999999999999999999999999999,
              "Volume": "not-a-number"
            }
            """;
        _mockFileSystem.Setup(fs => fs.FileExists(@"C:\app\settings.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(@"C:\app\settings.json")).Returns(json);

        var settings = _service.Load();

        settings.Profiles[0].MinimumFileSize.Should().Be(1);
        settings.Profiles[0].Volume.Should().Be(0.5);
    }

    [Fact]
    public void Save_ShouldWriteJsonToFile()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);

        var settings = new AppSettings
        {
            Profiles = new List<ProfileSettings>
            {
                new() { Name = "Default", TargetPaths = new List<string> { @"C:\test" } },
            },
            ActiveProfileName = "Default",
        };

        _service.Save(settings);

        _mockFileSystem.Verify(
            fs => fs.WriteAllText(
                @"C:\app\settings.json",
                It.Is<string>(s => s.Contains("C:\\\\test") && s.Contains("Default"))),
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
    public void Save_BareFilename_ShouldNotAttemptCreateDirectory()
    {
        // When the settings path has no directory component (just a filename),
        // GetDirectoryName returns string.Empty — CreateDirectory must not be called.
        var bareService = new SettingsService(_mockFileSystem.Object, "settings.json");

        bareService.Save(new AppSettings());

        _mockFileSystem.Verify(fs => fs.DirectoryExists(It.IsAny<string>()), Times.Never);
        _mockFileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Save_DirectoryExists_ShouldNotCreateIt()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);

        _service.Save(new AppSettings());

        _mockFileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SaveAndLoad_MultiProfile_ShouldRoundTrip()
    {
        string? savedJson = null;
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\app")).Returns(true);
        _mockFileSystem.Setup(fs => fs.WriteAllText(@"C:\app\settings.json", It.IsAny<string>()))
            .Callback<string, string>((_, json) => savedJson = json);

        var settings = new AppSettings
        {
            Profiles = new List<ProfileSettings>
            {
                new()
                {
                    Name = "Work",
                    TargetPaths = new List<string> { @"C:\projects" },
                    IncludeSubdirectories = false,
                    FilterRules = new List<FilterRule>
                    {
                        new() { Pattern = "*.jpg", Action = FilterAction.Include, Target = FilterTarget.Filename, IsRegex = false, IgnoreCase = true, IsEnabled = true },
                    },
                },
                new() { Name = "Photos", TargetPaths = new List<string> { @"D:\photos" } },
            },
            ActiveProfileName = "Photos",
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

        loaded.Profiles.Should().HaveCount(2);
        loaded.ActiveProfileName.Should().Be("Photos");
        loaded.Profiles[0].Name.Should().Be("Work");
        loaded.Profiles[0].IncludeSubdirectories.Should().BeFalse();
        loaded.Profiles[0].FilterRules.Should().ContainSingle().Which.Pattern.Should().Be("*.jpg");
        loaded.Profiles[1].Name.Should().Be("Photos");
        loaded.WindowLeft.Should().Be(100.5);
        loaded.WindowTop.Should().Be(200.0);
        loaded.WindowWidth.Should().Be(1400.0);
        loaded.WindowHeight.Should().Be(900.0);
        loaded.IsMaximized.Should().BeTrue();
    }
}
