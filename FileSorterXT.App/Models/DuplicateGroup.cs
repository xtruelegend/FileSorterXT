namespace FileSorterXT.Models;

public class DuplicateGroup
{
    public string Key { get; set; } = "";
    public long Size { get; set; } = 0;
    public int Count => Files.Count;
    public List<DuplicateFile> Files { get; set; } = new();
}

public class DuplicateFile
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public string? Hash { get; set; }
}
