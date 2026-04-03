using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class ScanAnalyticsTests
{
    [Fact]
    public void FromResult_EmptyResult_ShouldReturnZeros()
    {
        var result = new ScanResult();

        var analytics = ScanAnalytics.FromResult(result);

        analytics.TotalFiles.Should().Be(0);
        analytics.TotalDuplicates.Should().Be(0);
        analytics.DuplicateGroups.Should().Be(0);
        analytics.WastedBytes.Should().Be(0);
        analytics.TotalSizeBytes.Should().Be(0);
        analytics.DuplicatePercentage.Should().Be(0);
        analytics.WastedPercentage.Should().Be(0);
        analytics.TopExtensions.Should().BeEmpty();
        analytics.SizeDistribution.Should().HaveCount(6);
        analytics.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void FromResult_WithDuplicates_ShouldComputeCorrectly()
    {
        var result = new ScanResult
        {
            TotalFilesScanned = 100,
            TotalDuplicates = 20,
            TotalWastedBytes = 5000,
            Duration = TimeSpan.FromSeconds(2.5),
            DuplicateGroups = new List<DuplicateGroup>
            {
                new()
                {
                    Hash = "abc",
                    FileSize = 500,
                    Files = new List<ScannedFile>
                    {
                        new() { FilePath = @"C:\a.txt", FileName = "a.txt", FileSize = 500 },
                        new() { FilePath = @"C:\b.txt", FileName = "b.txt", FileSize = 500 },
                    },
                },
                new()
                {
                    Hash = "def",
                    FileSize = 2000,
                    Files = new List<ScannedFile>
                    {
                        new() { FilePath = @"C:\c.pdf", FileName = "c.pdf", FileSize = 2000 },
                        new() { FilePath = @"C:\d.pdf", FileName = "d.pdf", FileSize = 2000 },
                        new() { FilePath = @"C:\e.pdf", FileName = "e.pdf", FileSize = 2000 },
                    },
                },
            },
        };

        var analytics = ScanAnalytics.FromResult(result);

        analytics.TotalFiles.Should().Be(100);
        analytics.TotalDuplicates.Should().Be(20);
        analytics.DuplicateGroups.Should().Be(2);
        analytics.WastedBytes.Should().Be(5000);
        analytics.TotalSizeBytes.Should().Be(500 + 500 + 2000 + 2000 + 2000);
        analytics.DuplicatePercentage.Should().Be(20.0);
        analytics.Duration.Should().Be(TimeSpan.FromSeconds(2.5));
    }

    [Fact]
    public void FromResult_ShouldComputeTopExtensions()
    {
        var result = new ScanResult
        {
            TotalFilesScanned = 10,
            TotalDuplicates = 6,
            DuplicateGroups = new List<DuplicateGroup>
            {
                new()
                {
                    Hash = "a", FileSize = 100,
                    Files = new List<ScannedFile>
                    {
                        new() { FileName = "a.txt", FileSize = 100 },
                        new() { FileName = "b.txt", FileSize = 100 },
                        new() { FileName = "c.pdf", FileSize = 100 },
                        new() { FileName = "d.pdf", FileSize = 100 },
                        new() { FileName = "e.pdf", FileSize = 100 },
                        new() { FileName = "f", FileSize = 100 },
                    },
                },
            },
        };

        var analytics = ScanAnalytics.FromResult(result);

        analytics.TopExtensions.Should().HaveCount(3);
        analytics.TopExtensions[0].Extension.Should().Be("PDF");
        analytics.TopExtensions[0].FileCount.Should().Be(3);
        analytics.TopExtensions[1].Extension.Should().Be("TXT");
        analytics.TopExtensions[2].Extension.Should().Be("(no ext)");
        analytics.TopExtensions[2].FileCount.Should().Be(1);
    }

    [Fact]
    public void FromResult_ShouldComputeSizeDistribution()
    {
        var result = new ScanResult
        {
            TotalFilesScanned = 3,
            TotalDuplicates = 3,
            DuplicateGroups = new List<DuplicateGroup>
            {
                new()
                {
                    Hash = "a", FileSize = 500,
                    Files = new List<ScannedFile>
                    {
                        new() { FileName = "a.txt", FileSize = 500 },     // < 1 KB
                        new() { FileName = "b.txt", FileSize = 500000 }, // 100 KB – 1 MB
                        new() { FileName = "c.txt", FileSize = 5000000 }, // 1 MB – 10 MB
                    },
                },
            },
        };

        var analytics = ScanAnalytics.FromResult(result);

        analytics.SizeDistribution.Should().HaveCount(6);
        analytics.SizeDistribution[0].Label.Should().Be("< 1 KB");
        analytics.SizeDistribution[0].FileCount.Should().Be(1);
        analytics.SizeDistribution[2].Label.Should().Be("100 KB – 1 MB");
        analytics.SizeDistribution[2].FileCount.Should().Be(1);
        analytics.SizeDistribution[3].Label.Should().Be("1 MB – 10 MB");
        analytics.SizeDistribution[3].FileCount.Should().Be(1);
    }

    [Fact]
    public void FromResult_SizeDistribution_BarWidthRelativeToMax()
    {
        var result = new ScanResult
        {
            TotalFilesScanned = 5,
            TotalDuplicates = 5,
            DuplicateGroups = new List<DuplicateGroup>
            {
                new()
                {
                    Hash = "a", FileSize = 500,
                    Files = new List<ScannedFile>
                    {
                        new() { FileName = "1.txt", FileSize = 500 },
                        new() { FileName = "2.txt", FileSize = 500 },
                        new() { FileName = "3.txt", FileSize = 500 },
                        new() { FileName = "4.txt", FileSize = 500 },
                        new() { FileName = "5.txt", FileSize = 50000 },
                    },
                },
            },
        };

        var analytics = ScanAnalytics.FromResult(result);

        // 4 files in "< 1 KB", 1 in "1 KB – 100 KB"
        var smallBucket = analytics.SizeDistribution[0];
        var medBucket = analytics.SizeDistribution[1];
        smallBucket.FileCount.Should().Be(4);
        smallBucket.BarWidth.Should().Be(100); // Max bucket = 100%
        medBucket.FileCount.Should().Be(1);
        medBucket.BarWidth.Should().Be(25); // 1/4 of max
    }

    [Fact]
    public void FormattedTotalSize_ShouldFormat()
    {
        var analytics = new ScanAnalytics { TotalSizeBytes = 1048576 };
        analytics.FormattedTotalSize.Should().Be("1.0 MB");
    }

    [Fact]
    public void FormattedWastedSize_ShouldFormat()
    {
        var analytics = new ScanAnalytics { WastedBytes = 2048 };
        analytics.FormattedWastedSize.Should().Be("2.0 KB");
    }

    [Fact]
    public void UniqueFiles_ShouldCalculateCorrectly()
    {
        var result = new ScanResult
        {
            TotalFilesScanned = 50,
            TotalDuplicates = 10,
            DuplicateGroups = new List<DuplicateGroup>
            {
                new() { Files = new List<ScannedFile> { new(), new(), new() } },
                new() { Files = new List<ScannedFile> { new(), new() } },
            },
        };

        var analytics = ScanAnalytics.FromResult(result);

        // 50 total - 10 duplicates + 2 groups (one per group is "original")
        analytics.UniqueFiles.Should().Be(42);
    }
}

public class ExtensionStatTests
{
    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var stat = new ExtensionStat
        {
            Extension = "PDF",
            FileCount = 5,
            TotalSize = 1048576,
        };

        stat.Extension.Should().Be("PDF");
        stat.FileCount.Should().Be(5);
        stat.TotalSize.Should().Be(1048576);
        stat.FormattedSize.Should().Be("1.0 MB");
    }
}

public class SizeBucketTests
{
    [Fact]
    public void Properties_ShouldSetAndGet()
    {
        var bucket = new SizeBucket
        {
            Label = "< 1 KB",
            FileCount = 10,
            BarWidth = 75.5,
        };

        bucket.Label.Should().Be("< 1 KB");
        bucket.FileCount.Should().Be(10);
        bucket.BarWidth.Should().Be(75.5);
    }
}
