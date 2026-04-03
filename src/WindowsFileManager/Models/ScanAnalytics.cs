namespace WindowsFileManager.Models;

/// <summary>
/// Analytics computed from a scan result.
/// </summary>
public class ScanAnalytics
{
    /// <summary>
    /// Gets or sets the total files scanned.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the total duplicate files.
    /// </summary>
    public int TotalDuplicates { get; set; }

    /// <summary>
    /// Gets or sets the total unique files (non-duplicate).
    /// </summary>
    public int UniqueFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of duplicate groups.
    /// </summary>
    public int DuplicateGroups { get; set; }

    /// <summary>
    /// Gets or sets the total wasted bytes.
    /// </summary>
    public long WastedBytes { get; set; }

    /// <summary>
    /// Gets or sets the total size of all scanned files.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the scan duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the duplicate percentage (0-100).
    /// </summary>
    public double DuplicatePercentage { get; set; }

    /// <summary>
    /// Gets or sets the wasted space percentage (0-100).
    /// </summary>
    public double WastedPercentage { get; set; }

    /// <summary>
    /// Gets or sets the top duplicate extensions with counts.
    /// </summary>
    public List<ExtensionStat> TopExtensions { get; set; } = new();

    /// <summary>
    /// Gets or sets the size distribution buckets.
    /// </summary>
    public List<SizeBucket> SizeDistribution { get; set; } = new();

    /// <summary>
    /// Gets the formatted total size.
    /// </summary>
    public string FormattedTotalSize => ScannedFile.FormatFileSize(TotalSizeBytes);

    /// <summary>
    /// Gets the formatted wasted size.
    /// </summary>
    public string FormattedWastedSize => ScannedFile.FormatFileSize(WastedBytes);

    /// <summary>
    /// Builds analytics from a scan result.
    /// </summary>
    /// <param name="result">The scan result.</param>
    /// <returns>Computed analytics.</returns>
    public static ScanAnalytics FromResult(ScanResult result)
    {
        var allDuplicateFiles = result.DuplicateGroups.SelectMany(g => g.Files).ToList();
        var totalSize = allDuplicateFiles.Sum(f => f.FileSize);

        var analytics = new ScanAnalytics
        {
            TotalFiles = result.TotalFilesScanned,
            TotalDuplicates = result.TotalDuplicates,
            UniqueFiles = result.TotalFilesScanned - result.TotalDuplicates
                + result.DuplicateGroups.Count, // one per group is the "original"
            DuplicateGroups = result.DuplicateGroups.Count,
            WastedBytes = result.TotalWastedBytes,
            TotalSizeBytes = totalSize,
            Duration = result.Duration,
        };

        analytics.DuplicatePercentage = result.TotalFilesScanned > 0
            ? (double)result.TotalDuplicates / result.TotalFilesScanned * 100
            : 0;

        analytics.WastedPercentage = totalSize > 0
            ? (double)result.TotalWastedBytes / totalSize * 100
            : 0;

        // Top extensions
        analytics.TopExtensions = allDuplicateFiles
            .GroupBy(f =>
            {
                var ext = System.IO.Path.GetExtension(f.FileName).TrimStart('.').ToUpperInvariant();
                return string.IsNullOrEmpty(ext) ? "(no ext)" : ext;
            })
            .Select(g => new ExtensionStat
            {
                Extension = g.Key,
                FileCount = g.Count(),
                TotalSize = g.Sum(f => f.FileSize),
            })
            .OrderByDescending(e => e.TotalSize)
            .Take(8)
            .ToList();

        // Size distribution
        analytics.SizeDistribution = BuildSizeDistribution(allDuplicateFiles);

        return analytics;
    }

    private static List<SizeBucket> BuildSizeDistribution(List<ScannedFile> files)
    {
        var buckets = new (string Label, long Min, long Max)[]
        {
            ("< 1 KB", 0, 1024),
            ("1 KB – 100 KB", 1024, 100 * 1024),
            ("100 KB – 1 MB", 100 * 1024, 1024 * 1024),
            ("1 MB – 10 MB", 1024 * 1024, 10L * 1024 * 1024),
            ("10 MB – 100 MB", 10L * 1024 * 1024, 100L * 1024 * 1024),
            ("100 MB+", 100L * 1024 * 1024, long.MaxValue),
        };

        var result = new List<SizeBucket>();
        var maxCount = 0;

        foreach (var (label, min, max) in buckets)
        {
            var count = files.Count(f => f.FileSize >= min && f.FileSize < max);
            if (count > maxCount)
            {
                maxCount = count;
            }

            result.Add(new SizeBucket
            {
                Label = label,
                FileCount = count,
            });
        }

        // Calculate bar widths relative to max
        foreach (var bucket in result)
        {
            bucket.BarWidth = maxCount > 0
                ? (double)bucket.FileCount / maxCount * 100
                : 0;
        }

        return result;
    }
}

/// <summary>
/// File extension statistics.
/// </summary>
public class ExtensionStat
{
    /// <summary>
    /// Gets or sets the file extension.
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of duplicate files with this extension.
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Gets or sets the total size of duplicates with this extension.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets the formatted total size.
    /// </summary>
    public string FormattedSize => ScannedFile.FormatFileSize(TotalSize);
}

/// <summary>
/// A size distribution bucket.
/// </summary>
public class SizeBucket
{
    /// <summary>
    /// Gets or sets the bucket label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of files in this bucket.
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Gets or sets the bar width percentage (0-100) relative to the largest bucket.
    /// </summary>
    public double BarWidth { get; set; }
}
