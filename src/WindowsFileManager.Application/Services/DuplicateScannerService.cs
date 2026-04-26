using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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

        // Step 2 + 3: group either by name regex (no size/hash check) or by size + content hash
        var duplicateGroups = !string.IsNullOrWhiteSpace(options.MatchRegex)
            ? GroupByNameRegex(scannedFiles, options.MatchRegex, cancellationToken)
            : GroupBySizeAndHash(scannedFiles, cancellationToken);

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

    private List<DuplicateGroup> GroupBySizeAndHash(List<ScannedFile> scannedFiles, CancellationToken cancellationToken)
    {
        var sizeGroups = scannedFiles
            .GroupBy(f => f.FileSize)
            .Where(g => g.Count() > 1);

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

        return duplicateGroups;
    }

    private static List<DuplicateGroup> GroupByNameRegex(List<ScannedFile> scannedFiles, string pattern, CancellationToken cancellationToken)
    {
        // Pattern is honored as-is — caller is responsible for inline flags like (?i) for case insensitivity.
        Regex regex;
        try
        {
            // 1-second per-match timeout guards against catastrophic backtracking from user typos
            // like `(.+)*` — without it the worker thread would hang since the cancellation token
            // is only checked between files, not inside Match itself.
            regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid duplicate-match regex: {ex.Message}", nameof(pattern), ex);
        }

        var keyGroups = new Dictionary<string, List<ScannedFile>>(StringComparer.Ordinal);

        foreach (var file in scannedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Match match;
            try
            {
                match = regex.Match(file.FileName);
            }
            catch (RegexMatchTimeoutException ex)
            {
                throw new ArgumentException(
                    $"Duplicate-match regex timed out on '{file.FileName}'. The pattern likely backtracks catastrophically — try a more specific regex.",
                    nameof(pattern),
                    ex);
            }

            if (!match.Success)
            {
                continue;
            }

            var key = BuildRegexKey(match);

            if (!keyGroups.TryGetValue(key, out var bucket))
            {
                bucket = new List<ScannedFile>();
                keyGroups[key] = bucket;
            }

            bucket.Add(file);
        }

        var duplicateGroups = new List<DuplicateGroup>();
        foreach (var (key, files) in keyGroups)
        {
            if (files.Count < 2)
            {
                continue;
            }

            var orderedFiles = files.OrderBy(f => f.FilePath).ToList();
            duplicateGroups.Add(new DuplicateGroup
            {
                Hash = key,
                FileSize = orderedFiles.Max(f => f.FileSize),
                Files = orderedFiles,
            });
        }

        return duplicateGroups;
    }

    private static string BuildRegexKey(Match match)
    {
        // groups[0] = full match. Use captures verbatim — the user controls case via inline regex
        // flags like (?i). Captures joined with SOH (0x01) so ('ab','c') and ('a','bc') stay distinct.
        if (match.Groups.Count > 1)
        {
            var parts = new string[match.Groups.Count - 1];
            for (var i = 1; i < match.Groups.Count; i++)
            {
                parts[i - 1] = match.Groups[i].Value;
            }

            return string.Join("", parts);
        }

        return match.Value;
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
            subDirs = _fileSystem.EnumerateDirectories(rootPath);
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
