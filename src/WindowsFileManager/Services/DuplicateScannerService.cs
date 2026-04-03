using System.Diagnostics;
using System.IO;
using WindowsFileManager.Models;

namespace WindowsFileManager.Services;

/// <summary>
/// Scans directories to find duplicate files.
/// Algorithm: group by size (fast filter) → hash only same-size files → group by hash.
/// </summary>
public class DuplicateScannerService
{
    private readonly IFileSystemService _fileSystem;
    private readonly FileHashService _hashService;

    internal DuplicateScannerService(IFileSystemService fileSystem, FileHashService hashService)
    {
        _fileSystem = fileSystem;
        _hashService = hashService;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateScannerService"/> class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public DuplicateScannerService()
        : this(new FileSystemService(), new FileHashService())
    {
    }

    /// <summary>
    /// Scans for duplicate files using the given options.
    /// </summary>
    /// <param name="options">Scan configuration.</param>
    /// <param name="progress">Optional progress callback (files scanned so far).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scan result with duplicate groups.</returns>
    public ScanResult Scan(ScanOptions options, Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (options.TargetPaths.Count == 0)
        {
            throw new ArgumentException("At least one target path is required.");
        }

        foreach (var path in options.TargetPaths)
        {
            if (!_fileSystem.DirectoryExists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }
        }

        var stopwatch = Stopwatch.StartNew();

        var searchOption = options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var allFiles = options.TargetPaths
            .SelectMany(path => _fileSystem.EnumerateFiles(path, "*.*", searchOption));

        // Step 1: Collect file metadata and apply filters
        var scannedFiles = new List<ScannedFile>();
        var filesScanned = 0;

        foreach (var filePath in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var size = _fileSystem.GetFileSize(filePath);
            if (size < options.MinimumFileSize)
            {
                continue;
            }

            var fileName = _fileSystem.GetFileName(filePath);

            if (options.FileExtensions.Count > 0)
            {
                var ext = Path.GetExtension(fileName).TrimStart('.');
                if (!options.FileExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
            }

            scannedFiles.Add(new ScannedFile
            {
                FilePath = filePath,
                FileName = fileName,
                FileSize = size,
                LastModified = _fileSystem.GetLastWriteTime(filePath),
            });

            filesScanned++;
            if (progress != null && filesScanned % 100 == 0)
            {
                progress.Invoke(filesScanned);
            }
        }

        // Report final count
        progress?.Invoke(filesScanned);

        // Step 2: Group by size — unique sizes cannot be duplicates
        var sizeGroups = scannedFiles
            .GroupBy(f => f.FileSize)
            .Where(g => g.Count() > 1);

        // Step 3: For same-size files, compute hash and group
        var duplicateGroups = new List<DuplicateGroup>();

        foreach (var sizeGroup in sizeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var file in sizeGroup)
            {
                file.Hash = _hashService.ComputeHash(file.FilePath);
            }

            var hashGroups = sizeGroup
                .GroupBy(f => f.Hash)
                .Where(g => g.Count() > 1);

            foreach (var hashGroup in hashGroups)
            {
                duplicateGroups.Add(new DuplicateGroup
                {
                    Hash = hashGroup.Key,
                    FileSize = sizeGroup.Key,
                    Files = hashGroup.OrderBy(f => f.FilePath).ToList(),
                });
            }
        }

        stopwatch.Stop();

        // Sort by wasted space descending
        duplicateGroups = duplicateGroups.OrderByDescending(g => g.WastedBytes).ToList();

        return new ScanResult
        {
            TotalFilesScanned = filesScanned,
            TotalDuplicates = duplicateGroups.Sum(g => g.Count),
            TotalWastedBytes = duplicateGroups.Sum(g => g.WastedBytes),
            DuplicateGroups = duplicateGroups,
            Duration = stopwatch.Elapsed,
        };
    }
}
