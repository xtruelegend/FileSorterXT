using System.Collections.Generic;

namespace FileSorterXT.Models;

public class AppSettings
{
    // Sorting
    public bool IncludeSubfolders { get; set; } = false;
    public bool PreserveStructure { get; set; } = false;
    public bool IncludeHiddenAndSystem { get; set; } = false;
    public ConfirmationMode ConfirmationMode { get; set; } = ConfirmationMode.Manual;
    public SortActionMode SortActionMode { get; set; } = SortActionMode.Move;

    // Per-extension destination overrides (extension includes leading dot, lowercase)
    public Dictionary<string, string> ExtensionDestinations { get; set; } = new();

// Default destination overrides (optional). If null/empty, KnownFolders defaults are used.
public string? DefaultPicturesFolder { get; set; } = null;
public string? DefaultDocumentsFolder { get; set; } = null;
public string? DefaultMusicFolder { get; set; } = null;
public string? DefaultVideosFolder { get; set; } = null;


    // Ignore lists
    public List<string> IgnoreExtensions { get; set; } = new()
    {
        ".lnk", ".url",
        ".exe", ".msi", ".msix", ".appx", ".appxbundle",
        ".bat", ".cmd", ".ps1"
    };

    public List<string> IgnorePaths { get; set; } = new();

    // Duplicates
    public DuplicateDefinition DuplicateDefinition { get; set; } = DuplicateDefinition.FilenameAndSize;
    public DuplicateAction DuplicateAction { get; set; } = DuplicateAction.DoNotMove;
    public string? DuplicateCustomFolder { get; set; } = null;
    public KeepRule KeepRule { get; set; } = KeepRule.KeepNewest;
    public bool DuplicateActionRequiresConfirm { get; set; } = true;

    // Quick/Accurate selection when DuplicateDefinition = QuickAccurateModes
    public QuickAccurateMode QuickAccurateMode { get; set; } = QuickAccurateMode.Quick;

    // Duplicate scan scope
    public bool DuplicateScanDestinationsFromLastRun { get; set; } = true;
}
