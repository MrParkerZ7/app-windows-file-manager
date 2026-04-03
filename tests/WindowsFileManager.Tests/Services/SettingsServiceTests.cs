using FluentAssertions;
using Moq;
using WindowsFileManager.Models;
using WindowsFileManager.Services;

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
}
