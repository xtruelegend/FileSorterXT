using System.IO;
using FileSorterXT.Models;

namespace FileSorterXT.Services;

public static class PlanService
{
public static (List<PlanItem> plan, Dictionary<string,int> unmappedCounts) BuildPlanMany(AppSettings settings, IEnumerable<string> sourceFolders)
{
    var all = new List<PlanItem>();
    var unmapped = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);

    foreach (var folder in sourceFolders)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) continue;

        var (plan, u) = BuildPlan(settings, folder);

        all.AddRange(plan);

        foreach (var kv in u)
        {
            unmapped.TryGetValue(kv.Key, out var existing);
            unmapped[kv.Key] = existing + kv.Value;
        }
    }

    return (all, unmapped);
}

    public static (List<PlanItem> plan, Dictionary<string,int> unmappedCounts) BuildPlan(AppSettings settings, string sourceFolder)
    {
        var items = new List<PlanItem>();
        var unmapped = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);

        var searchOption = settings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var file in Directory.EnumerateFiles(sourceFolder, "*", searchOption))
        {
            if (IgnoreService.ShouldIgnore(settings, file))
            {
                items.Add(new PlanItem
                {
                    SourcePath = file,
                    Extension = FileCategorizer.NormalizeExt(Path.GetExtension(file)),
                    Category = FileCategorizer.CategoryFor(Path.GetExtension(file)),
                    DestinationPath = null,
                    IsSkipped = true,
                    Reason = "Ignored by settings"
                });
                continue;
            }

            if (!settings.IncludeHiddenAndSystem && IgnoreService.IsHiddenOrSystem(file))
            {
                items.Add(new PlanItem
                {
                    SourcePath = file,
                    Extension = FileCategorizer.NormalizeExt(Path.GetExtension(file)),
                    Category = FileCategorizer.CategoryFor(Path.GetExtension(file)),
                    DestinationPath = null,
                    IsSkipped = true,
                    Reason = "Hidden/System skipped"
                });
                continue;
            }

            var ext = FileCategorizer.NormalizeExt(Path.GetExtension(file));
            var cat = FileCategorizer.CategoryFor(ext);

            var destFolder = DestinationResolver.ResolveDestinationFolder(settings, ext, cat);

            if (string.IsNullOrWhiteSpace(destFolder))
            {
                if (!string.IsNullOrWhiteSpace(ext))
                {
                    unmapped.TryGetValue(ext, out var c);
                    unmapped[ext] = c + 1;
                }

                items.Add(new PlanItem
                {
                    SourcePath = file,
                    Extension = ext,
                    Category = cat,
                    DestinationPath = null,
                    IsSkipped = true,
                    Reason = "No destination mapped"
                });
                continue;
            }

            var destPath = DestinationResolver.MakeDestinationPath(
                sourceFolder,
                file,
                destFolder,
                settings.PreserveStructure && settings.IncludeSubfolders);

            var srcRoot = Path.GetFullPath(sourceFolder).TrimEnd(Path.DirectorySeparatorChar);
            var destFolderFull = Path.GetFullPath(destFolder).TrimEnd(Path.DirectorySeparatorChar);

            if (string.Equals(srcRoot, destFolderFull, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new PlanItem
                {
                    SourcePath = file,
                    Extension = ext,
                    Category = cat,
                    DestinationPath = null,
                    IsSkipped = true,
                    Reason = "Same-folder guard"
                });
                continue;
            }

            if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new PlanItem
                {
                    SourcePath = file,
                    Extension = ext,
                    Category = cat,
                    DestinationPath = null,
                    IsSkipped = true,
                    Reason = "Already in destination path"
                });
                continue;
            }

            if (File.Exists(destPath))
            {
                var newDest = DestinationResolver.MakeNonCollidingFilePath(destPath);

                items.Add(new PlanItem
                {
                    SourcePath = file,
                    Extension = ext,
                    Category = cat,
                    DestinationPath = newDest,
                    ActionMode = settings.SortActionMode,
                    IsSkipped = false,
                    IsCollisionPossibleDuplicate = true,
                    Reason = $"Name collision: auto-renamed to {Path.GetFileName(newDest)}"
                });
                continue;
            }

            items.Add(new PlanItem
            {
                SourcePath = file,
                Extension = ext,
                Category = cat,
                DestinationPath = destPath,
                ActionMode = settings.SortActionMode,
                IsSkipped = false,
                Reason = settings.SortActionMode == SortActionMode.Copy ? "Copy" : "Move"
            });
        }

        return (items, unmapped);
    }

    public static long TotalBytesToProcess(List<PlanItem> plan)
    {
        long total = 0;
        foreach (var p in plan)
        {
            if (p.WillMoveOrCopy && File.Exists(p.SourcePath))
            {
                try { total += new FileInfo(p.SourcePath).Length; } catch { }
            }
        }
        return total;
    }
}
