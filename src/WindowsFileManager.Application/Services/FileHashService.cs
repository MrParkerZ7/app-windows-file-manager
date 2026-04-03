using System.Security.Cryptography;
using WindowsFileManager.Core.Services;

namespace WindowsFileManager.Application.Services;

/// <summary>
/// Computes file content hashes for duplicate detection.
/// </summary>
public class FileHashService
{
    private readonly IFileSystemService _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileHashService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system service.</param>
    public FileHashService(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Computes the SHA256 hash of a file's content.
    /// </summary>
    /// <param name="filePath">The file path to hash.</param>
    /// <returns>Hex string of the SHA256 hash.</returns>
    public string ComputeHash(string filePath)
    {
        using var stream = _fileSystem.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }
}
