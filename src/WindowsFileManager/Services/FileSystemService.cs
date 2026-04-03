using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WindowsFileManager.Services;

/// <summary>
/// Real file system implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class FileSystemService : IFileSystemService
{
    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(path, searchPattern, new EnumerationOptions
        {
            RecurseSubdirectories = searchOption == SearchOption.AllDirectories,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System,
        });
    }

    /// <inheritdoc/>
    public long GetFileSize(string filePath) => new FileInfo(filePath).Length;

    /// <inheritdoc/>
    public DateTime GetLastWriteTime(string filePath) => File.GetLastWriteTime(filePath);

    /// <inheritdoc/>
    public Stream OpenRead(string filePath) => File.OpenRead(filePath);

    /// <inheritdoc/>
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc/>
    public string GetFileName(string filePath) => Path.GetFileName(filePath);

    /// <inheritdoc/>
    public bool FileExists(string filePath) => File.Exists(filePath);

    /// <inheritdoc/>
    public string ReadAllText(string filePath) => File.ReadAllText(filePath);

    /// <inheritdoc/>
    public void WriteAllText(string filePath, string content) => File.WriteAllText(filePath, content);

    /// <inheritdoc/>
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
}
