using System.Text;
using FluentAssertions;
using Moq;
using WindowsFileManager.Models;
using WindowsFileManager.Services;

namespace WindowsFileManager.Tests.Services;

public class DuplicateScannerServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly FileHashService _hashService;
    private readonly DuplicateScannerService _service;

    public DuplicateScannerServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _hashService = new FileHashService(_mockFileSystem.Object);
        _service = new DuplicateScannerService(_mockFileSystem.Object, _hashService);
    }

    [Fact]
    public void Scan_EmptyTargetPaths_ShouldThrow()
    {
        var options = new ScanOptions { TargetPaths = new List<string>() };

        var act = () => _service.Scan(options);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one target path*");
    }

    [Fact]
    public void Scan_DirectoryNotFound_ShouldThrow()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\nonexistent" } };

        var act = () => _service.Scan(options);

        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage("*C:\\nonexistent*");
    }

    [Fact]
    public void Scan_EmptyDirectory_ShouldReturnEmptyResult()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\empty")).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(@"C:\empty", "*.*", SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\empty" } };
        var result = _service.Scan(options);

        result.TotalFilesScanned.Should().Be(0);
        result.DuplicateGroups.Should().BeEmpty();
        result.TotalDuplicates.Should().Be(0);
        result.TotalWastedBytes.Should().Be(0);
    }

    [Fact]
    public void Scan_NoDuplicates_ShouldReturnEmptyGroups()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("file1.txt", 100L, "content1"),
            ("file2.txt", 200L, "content2"),
            ("file3.txt", 300L, "content3"),
        });

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" } };
        var result = _service.Scan(options);

        result.TotalFilesScanned.Should().Be(3);
        result.DuplicateGroups.Should().BeEmpty();
    }

    [Fact]
    public void Scan_WithDuplicates_ShouldFindThem()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("a.txt", 100L, "same content"),
            ("b.txt", 100L, "same content"),
            ("c.txt", 200L, "unique"),
        });

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" } };
        var result = _service.Scan(options);

        result.DuplicateGroups.Should().HaveCount(1);
        result.DuplicateGroups[0].Files.Should().HaveCount(2);
        result.DuplicateGroups[0].FileSize.Should().Be(100);
        result.TotalDuplicates.Should().Be(2);
        result.TotalWastedBytes.Should().Be(100);
    }

    [Fact]
    public void Scan_SameSizeDifferentContent_ShouldNotBeDuplicates()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("a.txt", 100L, "content A"),
            ("b.txt", 100L, "content B"),
        });

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" } };
        var result = _service.Scan(options);

        result.DuplicateGroups.Should().BeEmpty();
    }

    [Fact]
    public void Scan_MinimumFileSize_ShouldFilterSmallFiles()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("tiny.txt", 10L, "same"),
            ("tiny2.txt", 10L, "same"),
            ("big.txt", 1000L, "same big"),
            ("big2.txt", 1000L, "same big"),
        });

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" }, MinimumFileSize = 100 };
        var result = _service.Scan(options);

        result.TotalFilesScanned.Should().Be(2);
        result.DuplicateGroups.Should().HaveCount(1);
        result.DuplicateGroups[0].FileSize.Should().Be(1000);
    }

    [Fact]
    public void Scan_FileExtensionFilter_ShouldFilterByExtension()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("a.txt", 100L, "same"),
            ("b.txt", 100L, "same"),
            ("c.pdf", 100L, "same"),
        });

        var options = new ScanOptions
        {
            TargetPaths = new List<string> { @"C:\test" },
            FileExtensions = new List<string> { "txt" },
        };
        var result = _service.Scan(options);

        result.TotalFilesScanned.Should().Be(2);
        result.DuplicateGroups.Should().HaveCount(1);
        result.DuplicateGroups[0].Files.Should().HaveCount(2);
    }

    [Fact]
    public void Scan_TopDirectoryOnly_ShouldNotRecurse()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\test")).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(@"C:\test", "*.*", SearchOption.TopDirectoryOnly))
            .Returns(new[] { @"C:\test\a.txt" });
        _mockFileSystem.Setup(fs => fs.GetFileSize(@"C:\test\a.txt")).Returns(100L);
        _mockFileSystem.Setup(fs => fs.GetFileName(@"C:\test\a.txt")).Returns("a.txt");
        _mockFileSystem.Setup(fs => fs.GetLastWriteTime(It.IsAny<string>())).Returns(DateTime.Now);

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" }, IncludeSubdirectories = false };
        var result = _service.Scan(options);

        _mockFileSystem.Verify(fs => fs.EnumerateFiles(@"C:\test", "*.*", SearchOption.TopDirectoryOnly), Times.Once);
        result.TotalFilesScanned.Should().Be(1);
    }

    [Fact]
    public void Scan_ProgressCallback_ShouldReport()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("a.txt", 100L, "x"),
            ("b.txt", 200L, "y"),
        });

        var progressValues = new List<int>();
        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" } };
        _service.Scan(options, count => progressValues.Add(count));

        progressValues.Should().Contain(2);
        progressValues.Last().Should().Be(2);
    }

    [Fact]
    public void Scan_ProgressCallback_ShouldThrottleEvery100Files()
    {
        var files = Enumerable.Range(1, 250)
            .Select(i => ($"file{i}.txt", (long)i, $"content{i}"))
            .ToArray();
        SetupDirectory(@"C:\big", files);

        var progressValues = new List<int>();
        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\big" } };
        _service.Scan(options, count => progressValues.Add(count));

        progressValues.Should().Contain(100);
        progressValues.Should().Contain(200);
        progressValues.Last().Should().Be(250);
    }

    [Fact]
    public void Scan_Cancellation_ShouldThrow()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\test")).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(@"C:\test", "*.*", SearchOption.AllDirectories))
            .Returns(new[] { @"C:\test\a.txt", @"C:\test\b.txt" });
        _mockFileSystem.Setup(fs => fs.GetFileSize(It.IsAny<string>())).Returns(100L);
        _mockFileSystem.Setup(fs => fs.GetFileName(It.IsAny<string>())).Returns("a.txt");
        _mockFileSystem.Setup(fs => fs.GetLastWriteTime(It.IsAny<string>())).Returns(DateTime.Now);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" } };
        var act = () => _service.Scan(options, cancellationToken: cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Scan_MultipleDuplicateGroups_ShouldSortByWastedSpace()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("small1.txt", 100L, "small same"),
            ("small2.txt", 100L, "small same"),
            ("big1.txt", 10000L, "big same"),
            ("big2.txt", 10000L, "big same"),
        });

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" } };
        var result = _service.Scan(options);

        result.DuplicateGroups.Should().HaveCount(2);
        result.DuplicateGroups[0].FileSize.Should().Be(10000);
        result.DuplicateGroups[1].FileSize.Should().Be(100);
    }

    [Fact]
    public void Scan_DuplicateFiles_ShouldBeSortedByPath()
    {
        SetupDirectory(@"C:\test", new[]
        {
            (@"z\file.txt", 100L, "same"),
            (@"a\file.txt", 100L, "same"),
        });

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\test" } };
        var result = _service.Scan(options);

        result.DuplicateGroups[0].Files[0].FilePath.Should().Contain("a");
        result.DuplicateGroups[0].Files[1].FilePath.Should().Contain("z");
    }

    [Fact]
    public void Scan_FileExtensionFilter_CaseInsensitive()
    {
        SetupDirectory(@"C:\test", new[]
        {
            ("a.TXT", 100L, "same"),
            ("b.txt", 100L, "same"),
        });

        var options = new ScanOptions
        {
            TargetPaths = new List<string> { @"C:\test" },
            FileExtensions = new List<string> { "txt" },
        };
        var result = _service.Scan(options);

        result.TotalFilesScanned.Should().Be(2);
    }

    [Fact]
    public void Scan_MultipleFolders_ShouldFindCrossFolderDuplicates()
    {
        SetupDirectory(@"C:\folder1", new[]
        {
            ("a.txt", 100L, "shared content"),
        });
        SetupDirectory(@"C:\folder2", new[]
        {
            ("b.txt", 100L, "shared content"),
        });

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\folder1", @"C:\folder2" } };
        var result = _service.Scan(options);

        result.TotalFilesScanned.Should().Be(2);
        result.DuplicateGroups.Should().HaveCount(1);
        result.DuplicateGroups[0].Files.Should().HaveCount(2);
    }

    [Fact]
    public void Scan_MultipleFolders_OneNotFound_ShouldThrow()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\exists")).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(@"C:\missing")).Returns(false);

        var options = new ScanOptions { TargetPaths = new List<string> { @"C:\exists", @"C:\missing" } };
        var act = () => _service.Scan(options);

        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage("*C:\\missing*");
    }

    private void SetupDirectory(string path, (string name, long size, string content)[] files)
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(path)).Returns(true);

        var filePaths = files.Select(f => Path.Combine(path, f.name)).ToArray();
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(path, "*.*", It.IsAny<SearchOption>()))
            .Returns(filePaths);

        foreach (var (name, size, content) in files)
        {
            var fullPath = Path.Combine(path, name);
            _mockFileSystem.Setup(fs => fs.GetFileSize(fullPath)).Returns(size);
            _mockFileSystem.Setup(fs => fs.GetFileName(fullPath)).Returns(Path.GetFileName(name));
            _mockFileSystem.Setup(fs => fs.GetLastWriteTime(fullPath)).Returns(DateTime.Now);
            _mockFileSystem.Setup(fs => fs.OpenRead(fullPath))
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
    }
}
