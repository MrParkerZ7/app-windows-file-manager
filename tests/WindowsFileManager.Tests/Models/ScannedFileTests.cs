using FluentAssertions;
using WindowsFileManager.Models;

namespace WindowsFileManager.Tests.Models;

public class ScannedFileTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var file = new ScannedFile();

        file.FilePath.Should().BeEmpty();
        file.FileName.Should().BeEmpty();
        file.FileSize.Should().Be(0);
        file.Hash.Should().BeEmpty();
        file.LastModified.Should().Be(default);
    }

    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var now = DateTime.Now;
        var file = new ScannedFile
        {
            FilePath = @"C:\test\file.txt",
            FileName = "file.txt",
            FileSize = 1024,
            Hash = "ABC123",
            LastModified = now,
        };

        file.FilePath.Should().Be(@"C:\test\file.txt");
        file.FileName.Should().Be("file.txt");
        file.FileSize.Should().Be(1024);
        file.Hash.Should().Be("ABC123");
        file.LastModified.Should().Be(now);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1572864, "1.5 MB")]
    [InlineData(1073741824, "1.00 GB")]
    [InlineData(1610612736, "1.50 GB")]
    public void FormattedSize_ShouldFormatCorrectly(long bytes, string expected)
    {
        var file = new ScannedFile { FileSize = bytes };
        file.FormattedSize.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.00 GB")]
    public void FormatFileSize_ShouldFormatCorrectly(long bytes, string expected)
    {
        ScannedFile.FormatFileSize(bytes).Should().Be(expected);
    }
}
