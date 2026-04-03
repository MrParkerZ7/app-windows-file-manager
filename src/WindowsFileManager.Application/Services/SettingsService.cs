using System.IO;
using System.Text.Json;
using WindowsFileManager.Core.Models;
using WindowsFileManager.Core.Services;

namespace WindowsFileManager.Application.Services;

/// <summary>
/// Persists and loads application settings to/from JSON.
/// </summary>
public class SettingsService
{
    private readonly IFileSystemService _fileSystem;
    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system service.</param>
    /// <param name="settingsPath">The settings file path.</param>
    public SettingsService(IFileSystemService fileSystem, string settingsPath)
    {
        _fileSystem = fileSystem;
        _settingsPath = settingsPath;
    }

    /// <summary>
    /// Loads settings from disk. Returns defaults if file doesn't exist.
    /// </summary>
    /// <returns>The loaded or default settings.</returns>
    public AppSettings Load()
    {
        if (!_fileSystem.FileExists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = _fileSystem.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
        {
            _fileSystem.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        _fileSystem.WriteAllText(_settingsPath, json);
    }
}
