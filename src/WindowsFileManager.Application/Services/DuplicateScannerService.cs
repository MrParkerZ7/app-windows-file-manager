using System.Diagnostics;
using System.IO;
using WindowsFileManager.Core.Models;
using WindowsFileManager.Core.Services;

namespace WindowsFileManager.Application.Services;

/// <summary>
/// Scans directories to find duplicate files.
/// Algorithm: group by size (fast filter) -> hash only same-size files -> group by hash.
/// </summary>
public class DuplicateScannerService
{
    private readonly IFileSystemService _fileSystem;
    private readonly FileHashService _hashService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateScannerService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system service.</param>
    /// <param name="hashService">The file hash service.</param>
    public DuplicateScannerService(IFileSystemService fileSystem, FileHashService hashService)
    {
        _fileSystem = fileSystem;
        _hashService = hashService;
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

        var hasExclusions = options.ExcludeFolderNames.Count > 0;
        var excludeSet = hasExclusions
            ? new HashSet<string>(options.ExcludeFolderNames, StringComparer.OrdinalIgnoreCase)
            : null;

        IEnumerable<string> allFiles;
        if (hasExclusions && options.IncludeSubdirectories)
        {
            // Custom recursive enumeration that skips excluded folder names
            allFiles = options.TargetPaths
                .SelectMany(path => EnumerateFilesExcluding(path, excludeSet!))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var searchOption = options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            allFiles = options.TargetPaths
                .SelectMany(path => _fileSystem.EnumerateFiles(path, "*.*", searchOption))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

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

    private IEnumerable<string> EnumerateFilesExcluding(string rootPath, HashSet<string> excludeNames)
    {
        // Enumerate files in current directory
        foreach (var file in _fileSystem.EnumerateFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly))
        {
            yield return file;
        }

        // Recurse into subdirectories, skipping excluded names
        IEnumerable<string> subDirs;
        try
        {
            subDirs = Directory.EnumerateDirectories(rootPath);
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (var subDir in subDirs)
        {
            var dirName = Path.GetFileName(subDir);
            if (excludeNames.Contains(dirName))
            {
                continue;
            }

            foreach (var file in EnumerateFilesExcluding(subDir, excludeNames))
            {
                yield return file;
            }
        }
    }
}
