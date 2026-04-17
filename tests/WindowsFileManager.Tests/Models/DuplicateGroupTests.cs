using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class DuplicateGroupTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var group = new DuplicateGroup();

        group.Hash.Should().BeEmpty();
        group.FileSize.Should().Be(0);
        group.Files.Should().BeEmpty();
        group.Count.Should().Be(0);
        group.WastedBytes.Should().Be(0);
    }

    [Fact]
    public void Count_ShouldReturnFileCount()
    {
        var group = new DuplicateGroup
        {
            Files = new List<ScannedFile>
            {
                new() { FilePath = "a.txt" },
                new() { FilePath = "b.txt" },
                new() { FilePath = "c.txt" },
            },
        };

        group.Count.Should().Be(3);
    }

    [Fact]
    public void WastedBytes_ShouldCalculateCorrectly()
    {
        var group = new DuplicateGroup
        {
            FileSize = 1000,
            Files = new List<ScannedFile>
            {
                new() { FilePath = "a.txt" },
                new() { FilePath = "b.txt" },
                new() { FilePath = "c.txt" },
            },
        };

        // 3 files, keep 1, waste = 1000 * (3-1) = 2000
        group.WastedBytes.Should().Be(2000);
    }

    [Fact]
    public void FormattedWastedSize_ShouldFormatCorrectly()
    {
        var group = new DuplicateGroup
        {
            FileSize = 1048576, // 1 MB
            Files = new List<ScannedFile>
            {
                new() { FilePath = "a.txt" },
                new() { FilePath = "b.txt" },
            },
        };

        group.FormattedWastedSize.Should().Be("1.0 MB");
    }

    [Fact]
    public void FormattedFileSize_ShouldFormatCorrectly()
    {
        var group = new DuplicateGroup { FileSize = 2048 };

        group.FormattedFileSize.Should().Be("2.0 KB");
    }

    [Fact]
    public void FirstFilePath_Empty_ShouldReturnNull()
    {
        new DuplicateGroup().FirstFilePath.Should().BeNull();
    }

    [Fact]
    public void FirstFilePath_Populated_ShouldReturnFirstPath()
    {
        var group = new DuplicateGroup
        {
            Files = new List<ScannedFile>
            {
                new() { FilePath = @"C:\a.txt" },
                new() { FilePath = @"C:\b.txt" },
            },
        };

        group.FirstFilePath.Should().Be(@"C:\a.txt");
    }

    [Fact]
    public void FirstFileName_Empty_ShouldReturnEmpty()
    {
        new DuplicateGroup().FirstFileName.Should().BeEmpty();
    }

    [Fact]
    public void FirstFileName_Populated_ShouldReturnFirstName()
    {
        var group = new DuplicateGroup
        {
            Files = new List<ScannedFile>
            {
                new() { FileName = "photo.jpg" },
            },
        };

        group.FirstFileName.Should().Be("photo.jpg");
    }

    [Fact]
    public void FileExtension_Empty_ShouldReturnEmpty()
    {
        new DuplicateGroup().FileExtension.Should().BeEmpty();
    }

    [Fact]
    public void FileExtension_Populated_ShouldReturnLowercaseExt()
    {
        var group = new DuplicateGroup
        {
            Files = new List<ScannedFile> { new() { FilePath = @"C:\Photos\IMG.JPG" } },
        };

        group.FileExtension.Should().Be(".jpg");
    }

    [Fact]
    public void DeleteAllLabel_TwoFiles_ShouldSayDeleteBoth()
    {
        var group = new DuplicateGroup
        {
            Files = new List<ScannedFile>
            {
                new() { FilePath = "a" },
                new() { FilePath = "b" },
            },
        };

        group.DeleteAllLabel.Should().Be("🗑 Delete Both");
    }

    [Fact]
    public void DeleteAllLabel_ThreeOrMore_ShouldShowCount()
    {
        var group = new DuplicateGroup
        {
            Files = new List<ScannedFile>
            {
                new() { FilePath = "a" },
                new() { FilePath = "b" },
                new() { FilePath = "c" },
                new() { FilePath = "d" },
            },
        };

        group.DeleteAllLabel.Should().Be("🗑 Delete All (4)");
    }
}
