using System.IO;

namespace FileSorterXT.Services;

public static class TransferGuards
{
    public static string? GuardRiskySource(string sourceFolder)
    {
        var full = Path.GetFullPath(sourceFolder).TrimEnd(Path.DirectorySeparatorChar);

        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        bool risky =
            (!string.IsNullOrWhiteSpace(pf) && full.StartsWith(Path.GetFullPath(pf), StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(pf86) && full.StartsWith(Path.GetFullPath(pf86), StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(win) && full.StartsWith(Path.GetFullPath(win), StringComparison.OrdinalIgnoreCase));

        if (!risky) return null;

        return "This folder looks like an installed-program or system folder. Many installed apps cannot be moved safely by copying folders. Recommended: uninstall and reinstall, or use Windows Apps settings for supported apps.";
    }
}
