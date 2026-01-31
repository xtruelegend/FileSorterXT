using System.IO;

namespace FileSorterXT.Services;

public static class UndoService
{
    public static (int undone, int failed) UndoLastRun(string logFile)
    {
        var last = RunHistoryService.LoadLastRun();
        if (last.Actions.Count == 0) return (0, 0);

        int undone = 0, failed = 0;

        foreach (var a in last.Actions.OrderByDescending(x => x.WhenUtc))
        {
            try
            {
                if (a.ActionType.Equals("move", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(a.To)) continue;

                    var dir = Path.GetDirectoryName(a.From) ?? "";
                    if (!string.IsNullOrWhiteSpace(dir))
                        Directory.CreateDirectory(dir);

                    if (File.Exists(a.From))
                    {
                        failed++;
                        LogService.Append(logFile, $"{DateTime.Now:u} UNDO SKIP (exists) {a.To} -> {a.From}");
                        continue;
                    }

                    File.Move(a.To, a.From);
                    undone++;
                    LogService.Append(logFile, $"{DateTime.Now:u} UNDO move {a.To} -> {a.From}");
                }
                else if (a.ActionType.Equals("copy", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(a.To))
                    {
                        File.Delete(a.To);
                        undone++;
                        LogService.Append(logFile, $"{DateTime.Now:u} UNDO delete copy {a.To}");
                    }
                }
            }
            catch (Exception ex)
            {
                failed++;
                LogService.Append(logFile, $"{DateTime.Now:u} UNDO FAIL {a.To} : {ex.Message}");
            }
        }

        RunHistoryService.ClearLastRun();
        return (undone, failed);
    }
}
