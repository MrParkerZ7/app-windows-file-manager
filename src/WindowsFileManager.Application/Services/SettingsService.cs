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
    /// Loads settings from disk. Returns defaults if file doesn't exist. Migrates legacy flat settings into a Default profile.
    /// </summary>
    /// <returns>The loaded or default settings, always with at least one profile.</returns>
    public AppSettings Load()
    {
        if (!_fileSystem.FileExists(_settingsPath))
        {
            return CreateDefault();
        }

        AppSettings settings;
        string json;
        try
        {
            json = _fileSystem.ReadAllText(_settingsPath);
            settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return CreateDefault();
        }

        if (settings.Profiles.Count == 0)
        {
            var migrated = MigrateLegacyProfile(json);
            settings.Profiles.Add(migrated);
            settings.ActiveProfileName = migrated.Name;
        }

        if (string.IsNullOrEmpty(settings.ActiveProfileName) ||
            !settings.Profiles.Any(p => string.Equals(p.Name, settings.ActiveProfileName, StringComparison.OrdinalIgnoreCase)))
        {
            settings.ActiveProfileName = settings.Profiles[0].Name;
        }

        return settings;
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

    private static AppSettings CreateDefault()
    {
        var defaults = new AppSettings();
        defaults.Profiles.Add(new ProfileSettings { Name = "Default" });
        defaults.ActiveProfileName = "Default";
        return defaults;
    }

    private static ProfileSettings MigrateLegacyProfile(string json)
    {
        var profile = new ProfileSettings { Name = "Default" };

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return profile;
            }

            ReadStringList(root, "TargetPaths", profile.TargetPaths);
            ReadStringList(root, "DisabledTargetPaths", profile.DisabledTargetPaths);
            ReadStringList(root, "ExcludeFolderNames", profile.ExcludeFolderNames);
            ReadStringList(root, "DisabledExcludeFolderNames", profile.DisabledExcludeFolderNames);
            ReadStringList(root, "FolderSearchResultPaths", profile.FolderSearchResultPaths);
            ReadStringList(root, "SelectedFolderSearchResultPaths", profile.SelectedFolderSearchResultPaths);

            profile.IncludeSubdirectories = ReadBool(root, "IncludeSubdirectories", profile.IncludeSubdirectories);
            profile.IsMiniPreview = ReadBool(root, "IsMiniPreview", profile.IsMiniPreview);
            profile.IsAutoPreview = ReadBool(root, "IsAutoPreview", profile.IsAutoPreview);
            profile.IsAutoPlay = ReadBool(root, "IsAutoPlay", profile.IsAutoPlay);
            profile.MinimumFileSize = ReadLong(root, "MinimumFileSize", profile.MinimumFileSize);
            profile.Volume = ReadDouble(root, "Volume", profile.Volume);
            profile.SelectedSortOption = ReadString(root, "SelectedSortOption", profile.SelectedSortOption);
            profile.MoveTargetPath = ReadString(root, "MoveTargetPath", profile.MoveTargetPath);

            ReadObjectList(root, "FilterRules", profile.FilterRules);
            ReadObjectList(root, "FolderSearchPatterns", profile.FolderSearchPatterns);
        }
        catch (JsonException)
        {
            // Keep the defaults on malformed legacy JSON.
        }

        return profile;
    }

    private static void ReadStringList(JsonElement root, string name, List<string> target)
    {
        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (value != null)
                {
                    target.Add(value);
                }
            }
        }
    }

    private static void ReadObjectList<T>(JsonElement root, string name, List<T> target)
    {
        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in element.EnumerateArray())
        {
            var parsed = JsonSerializer.Deserialize<T>(item.GetRawText());
            if (parsed != null)
            {
                target.Add(parsed);
            }
        }
    }

    private static bool ReadBool(JsonElement root, string name, bool fallback)
    {
        if (root.TryGetProperty(name, out var element))
        {
            if (element.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (element.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return fallback;
    }

    private static long ReadLong(JsonElement root, string name, long fallback)
    {
        if (root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var value))
        {
            return value;
        }

        return fallback;
    }

    private static double ReadDouble(JsonElement root, string name, double fallback)
    {
        if (root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var value))
        {
            return value;
        }

        return fallback;
    }

    private static string ReadString(JsonElement root, string name, string fallback)
    {
        if (root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString()!;
        }

        return fallback;
    }
}
