using System.IO;

namespace FileSorterXT.Services;

public static class Paths
{
    public static string AppRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileSorterXT");

    public static string SettingsPath => Path.Combine(AppRoot, "settings.json");
    public static string LogsDir => Path.Combine(AppRoot, "Logs");
    public static string RunsDir => Path.Combine(AppRoot, "Runs");

    public static string DefaultDuplicatesFolder =>
        Path.Combine(KnownFolders.Documents, "FileSorterXT", "Duplicates");

    public static void Ensure()
    {
        Directory.CreateDirectory(AppRoot);
        Directory.CreateDirectory(LogsDir);
        Directory.CreateDirectory(RunsDir);
    }
}
