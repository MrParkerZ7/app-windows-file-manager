using System.IO;

namespace WindowsFileManager.Core.Services;

/// <summary>
/// Abstraction over file system operations for testability.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Enumerates files in a directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="searchPattern">The search pattern (e.g., "*.*").</param>
    /// <param name="searchOption">Whether to search subdirectories.</param>
    /// <returns>Enumerable of file paths.</returns>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>File size in bytes.</returns>
    long GetFileSize(string filePath);

    /// <summary>
    /// Gets the last write time of a file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The last write time.</returns>
    DateTime GetLastWriteTime(string filePath);

    /// <summary>
    /// Opens a file stream for reading.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>A readable stream.</returns>
    Stream OpenRead(string filePath);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>True if the directory exists.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Gets the file name from a full path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file name.</returns>
    string GetFileName(string filePath);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>True if the file exists.</returns>
    bool FileExists(string filePath);

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file contents.</returns>
    string ReadAllText(string filePath);

    /// <summary>
    /// Writes text to a file, creating or overwriting.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="content">The content to write.</param>
    void WriteAllText(string filePath, string content);

    /// <summary>
    /// Creates a directory (and parents) if it doesn't exist.
    /// </summary>
    /// <param name="path">The directory path.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Enumerates subdirectories in a directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>Enumerable of directory paths.</returns>
    IEnumerable<string> EnumerateDirectories(string path);
}
