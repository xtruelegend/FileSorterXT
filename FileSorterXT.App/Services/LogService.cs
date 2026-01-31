using System.IO;

namespace FileSorterXT.Services;

public static class LogService
{
    public static string NewLogFile(string prefix)
    {
        Paths.Ensure();
        var name = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        return Path.Combine(Paths.LogsDir, name);
    }

    public static void Append(string logFile, string line)
    {
        try { File.AppendAllText(logFile, line + Environment.NewLine); } catch { }
    }
}
