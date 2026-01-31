using System.IO;
using FileSorterXT.Models;
using System;

namespace FileSorterXT.Services;

public static class DestinationResolver
{
    public static string? ResolveDestinationFolder(AppSettings settings, string extension, FileCategory category)
    {
        var ext = FileCategorizer.NormalizeExt(extension);

        if (!string.IsNullOrWhiteSpace(ext) && settings.ExtensionDestinations.TryGetValue(ext, out var custom))
        {
            if (!string.IsNullOrWhiteSpace(custom)) return custom;
        }

        return category switch
        {
            FileCategory.Image => KnownFolders.Pictures,
            FileCategory.Document => KnownFolders.Documents,
            FileCategory.Music => KnownFolders.Music,
            FileCategory.Video => KnownFolders.Videos,
            _ => null
        };
    }



public static string MakeNonCollidingFilePath(string desiredPath)
{
    if (!File.Exists(desiredPath)) return desiredPath;

    var dir = Path.GetDirectoryName(desiredPath) ?? "";
    var name = Path.GetFileNameWithoutExtension(desiredPath);
    var ext = Path.GetExtension(desiredPath);

    for (int i = 1; i < 1000; i++)
    {
        var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
        if (!File.Exists(candidate)) return candidate;
    }

    return Path.Combine(dir, $"{name} ({Guid.NewGuid():N}){ext}");
}

    public static string MakeDestinationPath(
        string sourceRoot,
        string sourceFilePath,
        string destFolder,
        bool preserveStructure)
    {
        var fileName = Path.GetFileName(sourceFilePath);

        if (!preserveStructure) return Path.Combine(destFolder, fileName);

        var relDir = Path.GetDirectoryName(Path.GetRelativePath(sourceRoot, sourceFilePath)) ?? "";
        relDir = relDir.Trim();
        if (string.IsNullOrWhiteSpace(relDir) || relDir == ".")
            return Path.Combine(destFolder, fileName);

        return Path.Combine(destFolder, relDir, fileName);
    }
}