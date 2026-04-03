using FluentAssertions;
using WindowsFileManager.Models;

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
}
