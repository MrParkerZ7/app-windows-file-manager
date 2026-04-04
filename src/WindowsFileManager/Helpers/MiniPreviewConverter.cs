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
/// Converts a file path to a thumbnail image.
/// Images: direct BitmapImage (fast, reliable).
/// Video/other: Windows Shell IShellItemImageFactory for real thumbnails.
/// Based on pinvoke.net and ShellThumbs patterns.
/// </summary>
[ExcludeFromCodeCoverage]
[ValueConversion(typeof(string), typeof(ImageSource))]
public class MiniPreviewConverter : IValueConverter
{
    private static readonly HashSet<string> DirectLoadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jif", ".jfif", ".jpe",
        ".png", ".bmp", ".dib", ".gif",
        ".tiff", ".tif", ".ico",
        ".wdp", ".hdp", ".jxr",
    };

    // IShellItem GUID — this is what SHCreateItemFromParsingName returns
    private static readonly Guid ShellItemGuid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        // For native image formats, load directly — fast and reliable
        if (DirectLoadExtensions.Contains(ext))
        {
            return LoadBitmapThumbnail(filePath);
        }

        // For everything else (video, docs, etc.), try Shell thumbnail
        return GetShellThumbnail(filePath);
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static BitmapImage? LoadBitmapThumbnail(string filePath)
    {
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

    private static BitmapSource? GetShellThumbnail(string filePath)
    {
        IntPtr hBitmap = IntPtr.Zero;
        IShellItem? nativeShellItem = null;

        try
        {
            // Step 1: Create IShellItem from file path (using IShellItem GUID)
            var guid = ShellItemGuid;
            int hr = SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref guid, out nativeShellItem);

            if (hr != 0 || nativeShellItem == null)
            {
                return null;
            }

            // Step 2: Cast IShellItem to IShellItemImageFactory
            if (nativeShellItem is not IShellItemImageFactory factory)
            {
                return null;
            }

            // Step 3: Get the thumbnail (flags 0x0 = default)
            var size = new NativeSize(80, 80);
            hr = factory.GetImage(size, 0x0, out hBitmap);

            if (hr != 0 || hBitmap == IntPtr.Zero)
            {
                return null;
            }

            // Step 4: Convert HBITMAP to WPF BitmapSource
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }

            if (nativeShellItem != null)
            {
                Marshal.ReleaseComObject(nativeShellItem);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int Width;
        public int Height;

        public NativeSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    // IShellItem interface
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);

        void GetParent(out IShellItem ppsi);

        void GetDisplayName(uint sigdnName, out IntPtr ppszName);

        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    // IShellItemImageFactory interface — obtained by casting IShellItem
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeSize size, int flags, out IntPtr phbm);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    [DllImport("gdi32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
}
