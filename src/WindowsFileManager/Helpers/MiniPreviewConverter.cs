using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WindowsFileManager.Helpers;

/// <summary>
/// Converts a file path to a thumbnail using Windows Shell API.
/// Works for images, videos, documents — anything Windows Explorer can thumbnail.
/// </summary>
[ExcludeFromCodeCoverage]
[ValueConversion(typeof(string), typeof(ImageSource))]
public class MiniPreviewConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static MiniPreviewConverter Instance { get; } = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            return GetShellThumbnail(filePath, 80);
        }
        catch
        {
            // Fallback: try WPF BitmapImage for native image formats
            return TryLoadAsBitmap(filePath);
        }
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static ImageSource? GetShellThumbnail(string filePath, int size)
    {
        var hr = NativeMethods.SHCreateItemFromParsingName(
            filePath, IntPtr.Zero, typeof(NativeMethods.IShellItemImageFactory).GUID, out var shellItem);

        if (hr != 0 || shellItem == null)
        {
            return null;
        }

        try
        {
            var factory = (NativeMethods.IShellItemImageFactory)shellItem;
            var nativeSize = new NativeMethods.NativeSize { Width = size, Height = size };
            hr = factory.GetImage(nativeSize, NativeMethods.SIIGBF_THUMBNAILONLY, out var hBitmap);

            if (hr != 0)
            {
                // Fallback: allow icon-based thumbnail
                hr = factory.GetImage(nativeSize, NativeMethods.SIIGBF_BIGGERSIZEOK, out hBitmap);
            }

            if (hr != 0 || hBitmap == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(shellItem);
        }
    }

    private static BitmapImage? TryLoadAsBitmap(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".tif" or ".ico"))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 80;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static class NativeMethods
    {
        public const int SIIGBF_THUMBNAILONLY = 0x04;
        public const int SIIGBF_BIGGERSIZEOK = 0x01;

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeSize
        {
            public int Width;
            public int Height;
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        public interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(NativeSize size, int flags, out IntPtr phbm);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int SHCreateItemFromParsingName(
            string pszPath, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppv);

        [DllImport("gdi32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
