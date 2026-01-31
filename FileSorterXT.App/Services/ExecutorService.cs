using System.IO;
using FileSorterXT.Models;

namespace FileSorterXT.Services;

public static class ExecutorService
{
    public static (List<RunAction> actions, int movedOrCopied, int skipped, int failed) ExecuteOne(
        AppSettings settings,
        PlanItem item,
        string logFile,
        CancellationToken token)
    {
        var actions = new List<RunAction>();

        token.ThrowIfCancellationRequested();

        if (!item.WillMoveOrCopy || string.IsNullOrWhiteSpace(item.DestinationPath))
            return (actions, 0, 1, 0);

        try
        {
            var destDir = Path.GetDirectoryName(item.DestinationPath) ?? "";
            if (!string.IsNullOrWhiteSpace(destDir))
                Directory.CreateDirectory(destDir);


// Safety: if destination already exists (race or external changes), auto-rename
if (File.Exists(item.DestinationPath))
{
    var newDest = DestinationResolver.MakeNonCollidingFilePath(item.DestinationPath);
    item.DestinationPath = newDest;
    var nd = Path.GetDirectoryName(item.DestinationPath) ?? "";
    if (!string.IsNullOrWhiteSpace(nd))
        Directory.CreateDirectory(nd);
}

            if (settings.SortActionMode == SortActionMode.Copy)
            {
                File.Copy(item.SourcePath, item.DestinationPath, overwrite: false);
                actions.Add(new RunAction { ActionType = "copy", From = item.SourcePath, To = item.DestinationPath, WhenUtc = DateTime.UtcNow });
            }
            else
            {
                File.Move(item.SourcePath, item.DestinationPath);
                actions.Add(new RunAction { ActionType = "move", From = item.SourcePath, To = item.DestinationPath, WhenUtc = DateTime.UtcNow });
            }

            LogService.Append(logFile, $"{DateTime.Now:u} {settings.SortActionMode} {item.SourcePath} -> {item.DestinationPath}");
            return (actions, 1, 0, 0);
        }
        catch (IOException ioex)
        {
            LogService.Append(logFile, $"{DateTime.Now:u} FAIL {item.SourcePath} : {ioex.Message}");
            return (actions, 0, 0, 1);
        }
        catch (UnauthorizedAccessException uaex)
        {
            LogService.Append(logFile, $"{DateTime.Now:u} FAIL {item.SourcePath} : {uaex.Message}");
            return (actions, 0, 0, 1);
        }
        catch (Exception ex)
        {
            LogService.Append(logFile, $"{DateTime.Now:u} FAIL {item.SourcePath} : {ex.Message}");
            return (actions, 0, 0, 1);
        }
    }
}
