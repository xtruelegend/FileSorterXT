namespace FileSorterXT.Models;

public class RunAction
{
    public string ActionType { get; set; } = "move"; // move or copy
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public DateTime WhenUtc { get; set; } = DateTime.UtcNow;
}
