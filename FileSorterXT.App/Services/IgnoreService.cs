using System.IO;
using FileSorterXT.Models;

namespace FileSorterXT.Services;

public static class IgnoreService
{
    public static bool ShouldIgnore(AppSettings settings, string filePath)
    {
        var ext = FileCategorizer.NormalizeExt(Path.GetExtension(filePath));
        if (settings.IgnoreExtensions.Contains(ext))
            return true;

        var full = Path.GetFullPath(filePath);

        var appDir = Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? "";
        if (!string.IsNullOrWhiteSpace(appDir))
        {
            var appDirFull = Path.GetFullPath(appDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (full.StartsWith(appDirFull, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var p in settings.IgnorePaths)
        {
            try
            {
                var ip = Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (full.StartsWith(ip, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }
        }

        return false;
    }

    public static bool IsHiddenOrSystem(string filePath)
    {
        try
        {
            var attr = File.GetAttributes(filePath);
            return attr.HasFlag(FileAttributes.Hidden) || attr.HasFlag(FileAttributes.System);
        }
        catch
        {
            return false;
        }
    }
}
