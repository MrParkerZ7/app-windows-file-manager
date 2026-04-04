using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace WindowsFileManager.Helpers;

/// <summary>
/// Converts a file path to a file type emoji icon string.
/// </summary>
[ExcludeFromCodeCoverage]
[ValueConversion(typeof(string), typeof(string))]
public class FileTypeIconConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static FileTypeIconConverter Instance { get; } = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
        {
            return "📄";
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".mp4" or ".avi" or ".mkv" or ".wmv" or ".mov" or ".flv" or ".webm" or ".mpg" or ".mpeg" => "🎬",
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a" or ".opus" => "🎵",
            ".pdf" => "📕",
            ".doc" or ".docx" or ".odt" or ".rtf" => "📝",
            ".xls" or ".xlsx" or ".ods" or ".csv" => "📊",
            ".ppt" or ".pptx" or ".odp" => "📰",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
            ".exe" or ".dll" or ".msi" => "⚙️",
            ".ttf" or ".otf" or ".woff" or ".woff2" => "🔤",
            ".db" or ".sqlite" or ".mdf" => "🗄️",
            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".svg" or ".webp" or ".ico" => "🖼️",
            ".html" or ".htm" or ".css" or ".js" or ".ts" => "🌐",
            ".cs" or ".java" or ".py" or ".cpp" or ".c" or ".go" or ".rs" => "💻",
            ".json" or ".xml" or ".yaml" or ".yml" or ".toml" => "📋",
            ".txt" or ".log" or ".md" => "📄",
            _ => "📄",
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
