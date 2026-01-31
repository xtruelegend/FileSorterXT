namespace FileSorterXT.Models;

public class PlanItem
{
    public string SourcePath { get; set; } = "";
    public string Extension { get; set; } = "";
    public FileCategory Category { get; set; } = FileCategory.Other;

    // If null or empty, the item is skipped
    public string? DestinationPath { get; set; }

    public SortActionMode ActionMode { get; set; } = SortActionMode.Move;
    public bool WillMoveOrCopy => !string.IsNullOrWhiteSpace(DestinationPath);

    public bool IsSkipped { get; set; } = false;
    public bool IsCollisionPossibleDuplicate { get; set; } = false;
    public string Reason { get; set; } = "";
}
