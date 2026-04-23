using System;
using System.Diagnostics.CodeAnalysis;

namespace WindowsFileManager.Helpers;

[ExcludeFromCodeCoverage]
internal static class ShortcutHelper
{
    public static void CreateFolderShortcut(string shortcutPath, string targetFolderPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell COM component is not available.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            try
            {
                shortcut.TargetPath = targetFolderPath;
                shortcut.WorkingDirectory = targetFolderPath;
                shortcut.Save();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
            }
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }
}
