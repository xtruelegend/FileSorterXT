using System.IO;
using System.Security.Cryptography;
using FileSorterXT.Models;

namespace FileSorterXT.Services;

public static class DuplicateService
{
    public static List<DuplicateGroup> FindDuplicates(AppSettings settings, IEnumerable<string> foldersToScan, CancellationToken token)
    {
        var files = new List<DuplicateFile>();

        foreach (var folder in foldersToScan)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) continue;

            foreach (var path in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                token.ThrowIfCancellationRequested();

                if (IgnoreService.ShouldIgnore(settings, path)) continue;
                if (!settings.IncludeHiddenAndSystem && IgnoreService.IsHiddenOrSystem(path)) continue;

                try
                {
                    var fi = new FileInfo(path);
                    files.Add(new DuplicateFile
                    {
                        Path = fi.FullName,
                        Size = fi.Length,
                        ModifiedUtc = fi.LastWriteTimeUtc
                    });
                }
                catch { }
            }
        }

        return settings.DuplicateDefinition switch
        {
            DuplicateDefinition.FilenameOnly => GroupByFilenameOnly(files),
            DuplicateDefinition.FilenameAndSize => GroupByFilenameAndSize(files),
            DuplicateDefinition.HashMatch => GroupByHash(files, token),
            DuplicateDefinition.QuickAccurateModes => settings.QuickAccurateMode == QuickAccurateMode.Quick
                ? GroupByFilenameAndSize(files)
                : GroupByHash(files, token),
            _ => new List<DuplicateGroup>()
        };
    }

    private static List<DuplicateGroup> GroupByFilenameOnly(List<DuplicateFile> files)
    {
        return files
            .GroupBy(f => Path.GetFileName(f.Path), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup
            {
                Key = g.Key,
                Size = g.First().Size,
                Files = g.OrderByDescending(x => x.ModifiedUtc).ToList()
            })
            .OrderByDescending(g => g.Count)
            .ToList();
    }

    private static List<DuplicateGroup> GroupByFilenameAndSize(List<DuplicateFile> files)
    {
        return files
            .GroupBy(f => (Name: Path.GetFileName(f.Path).ToLowerInvariant(), f.Size))
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup
            {
                Key = $"{g.Key.Name}|{g.Key.Size}",
                Size = g.Key.Size,
                Files = g.OrderByDescending(x => x.ModifiedUtc).ToList()
            })
            .OrderByDescending(g => g.Size)
            .ToList();
    }

    private static List<DuplicateGroup> GroupByHash(List<DuplicateFile> files, CancellationToken token)
    {
        var sizeGroups = files.GroupBy(f => f.Size).Where(g => g.Count() > 1).ToList();
        var results = new List<DuplicateGroup>();

        foreach (var sg in sizeGroups)
        {
            token.ThrowIfCancellationRequested();

            var withHash = new List<DuplicateFile>();
            foreach (var f in sg)
            {
                token.ThrowIfCancellationRequested();
                f.Hash = ComputeSha256(f.Path);
                withHash.Add(f);
            }

            var hashGroups = withHash.GroupBy(f => f.Hash, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1);
            foreach (var hg in hashGroups)
            {
                var key = hg.Key ?? "unknown";
                results.Add(new DuplicateGroup
                {
                    Key = key,
                    Size = sg.Key,
                    Files = hg.OrderByDescending(x => x.ModifiedUtc).ToList()
                });
            }
        }

        return results.OrderByDescending(g => g.Size).ToList();
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream));
    }

    public static int ApplyDuplicateAction(AppSettings settings, List<DuplicateGroup> groups, string logFile, CancellationToken token)
    {
        if (settings.DuplicateAction == DuplicateAction.DoNotMove) return 0;

        var targetRoot = settings.DuplicateAction switch
        {
            DuplicateAction.MoveToDuplicatesFolder => Paths.DefaultDuplicatesFolder,
            DuplicateAction.MoveToCustomFolder => settings.DuplicateCustomFolder,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(targetRoot)) return 0;
        Directory.CreateDirectory(targetRoot);

        int moved = 0;

        foreach (var g in groups)
        {
            token.ThrowIfCancellationRequested();
            if (g.Files.Count < 2) continue;

            DuplicateFile keeper = settings.KeepRule switch
            {
                KeepRule.KeepNewest => g.Files.OrderByDescending(f => f.ModifiedUtc).First(),
                KeepRule.KeepOldest => g.Files.OrderBy(f => f.ModifiedUtc).First(),
                _ => g.Files.OrderByDescending(f => f.ModifiedUtc).First()
            };

            foreach (var f in g.Files)
            {
                token.ThrowIfCancellationRequested();

                if (string.Equals(f.Path, keeper.Path, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!File.Exists(f.Path)) continue;

                var destPath = SafeDestPath(targetRoot, Path.GetFileName(f.Path));
                try
                {
                    File.Move(f.Path, destPath);
                    moved++;
                    LogService.Append(logFile, $"{DateTime.Now:u} DUPLICATE move {f.Path} -> {destPath}");
                }
                catch (Exception ex)
                {
                    LogService.Append(logFile, $"{DateTime.Now:u} DUPLICATE FAIL {f.Path} : {ex.Message}");
                }
            }
        }

        return moved;
    }

    private static string SafeDestPath(string folder, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var candidate = Path.Combine(folder, fileName);

        int i = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(folder, $"{baseName} ({i}){ext}");
            i++;
        }
        return candidate;
    }
}
