using System.IO;
using System.Text.Json;
using FileSorterXT.Models;

namespace FileSorterXT.Services;

public static class RunHistoryService
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string LastRunPath => Path.Combine(Paths.RunsDir, "last_run.json");

    public static void SaveLastRun(List<RunAction> actions, List<string> destinationsUsed)
    {
        Paths.Ensure();
        var payload = new LastRunPayload
        {
            WhenUtc = DateTime.UtcNow,
            Actions = actions,
            DestinationsUsed = destinationsUsed
        };
        File.WriteAllText(LastRunPath, JsonSerializer.Serialize(payload, Opt));
    }

    public static LastRunPayload LoadLastRun()
    {
        Paths.Ensure();
        if (!File.Exists(LastRunPath)) return new LastRunPayload();
        var json = File.ReadAllText(LastRunPath);
        return JsonSerializer.Deserialize<LastRunPayload>(json, Opt) ?? new LastRunPayload();
    }

    public static void ClearLastRun()
    {
        Paths.Ensure();
        if (File.Exists(LastRunPath))
            File.Delete(LastRunPath);
    }
}

public class LastRunPayload
{
    public DateTime WhenUtc { get; set; } = DateTime.MinValue;
    public List<RunAction> Actions { get; set; } = new();
    public List<string> DestinationsUsed { get; set; } = new();
}
