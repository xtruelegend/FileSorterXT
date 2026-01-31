using System.IO;
using System.Text.Json;
using FileSorterXT.Models;

namespace FileSorterXT.Services;

public static class SettingsService
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static AppSettings Load()
    {
        Paths.Ensure();

        if (!File.Exists(Paths.SettingsPath))
        {
            var s = new AppSettings();
            Save(s);
            return s;
        }

        var json = File.ReadAllText(Paths.SettingsPath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, Opt) ?? new AppSettings();

        Normalize(settings);
        return settings;
    }

    public static void Save(AppSettings settings)
    {
        Paths.Ensure();
        Normalize(settings);

        var json = JsonSerializer.Serialize(settings, Opt);
        File.WriteAllText(Paths.SettingsPath, json);
    }

    private static void Normalize(AppSettings s)
    {
        var norm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in s.ExtensionDestinations)
        {
            var ext = kv.Key?.Trim() ?? "";
            if (!ext.StartsWith(".")) ext = "." + ext;
            ext = ext.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(ext) && !string.IsNullOrWhiteSpace(kv.Value))
                norm[ext] = kv.Value.Trim();
        }
        s.ExtensionDestinations = new Dictionary<string, string>(norm, StringComparer.OrdinalIgnoreCase);

        s.IgnoreExtensions = s.IgnoreExtensions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => (x.StartsWith(".") ? x : "." + x).ToLowerInvariant())
            .Distinct()
            .ToList();

        s.IgnorePaths = s.IgnorePaths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
