using System.Text;
using FluentAssertions;
using Moq;
using WindowsFileManager.Services;

namespace WindowsFileManager.Tests.Services;

public class FileHashServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly FileHashService _service;

    public FileHashServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _service = new FileHashService(_mockFileSystem.Object);
    }

    [Fact]
    public void ComputeHash_ShouldReturnConsistentHash()
    {
        var content = Encoding.UTF8.GetBytes("hello world");
        _mockFileSystem.Setup(fs => fs.OpenRead("file.txt"))
            .Returns(new MemoryStream(content));

        var hash1 = _service.ComputeHash("file.txt");

        _mockFileSystem.Setup(fs => fs.OpenRead("file.txt"))
            .Returns(new MemoryStream(content));

        var hash2 = _service.ComputeHash("file.txt");

        hash1.Should().NotBeEmpty();
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentContent_ShouldReturnDifferentHash()
    {
        _mockFileSystem.Setup(fs => fs.OpenRead("a.txt"))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes("content A")));

        var hashA = _service.ComputeHash("a.txt");

        _mockFileSystem.Setup(fs => fs.OpenRead("b.txt"))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes("content B")));

        var hashB = _service.ComputeHash("b.txt");

        hashA.Should().NotBe(hashB);
    }

    [Fact]
    public void ComputeHash_EmptyFile_ShouldReturnHash()
    {
        _mockFileSystem.Setup(fs => fs.OpenRead("empty.txt"))
            .Returns(new MemoryStream(Array.Empty<byte>()));

        var hash = _service.ComputeHash("empty.txt");

        hash.Should().NotBeEmpty();
    }
}
