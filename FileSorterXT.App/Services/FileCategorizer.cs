using System.Linq;
using FileSorterXT.Models;

namespace FileSorterXT.Services;

public static class FileCategorizer
{
    private static readonly HashSet<string> ImageExt = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg",".jpeg",".png",".gif",".webp",".bmp",".tiff",".svg",".heic",".avif" };

    private static readonly HashSet<string> DocExt = new(StringComparer.OrdinalIgnoreCase)
    { ".pdf",".doc",".docx",".txt",".rtf",".md",".xls",".xlsx",".ppt",".pptx",".csv" };

    private static readonly HashSet<string> MusicExt = new(StringComparer.OrdinalIgnoreCase)
    { ".mp3",".wav",".flac",".aac",".m4a",".ogg" };

    private static readonly HashSet<string> VideoExt = new(StringComparer.OrdinalIgnoreCase)
    { ".mp4",".mov",".mkv",".avi",".wmv",".webm" };

    public static FileCategory CategoryFor(string extension)
    {
        var ext = NormalizeExt(extension);
        if (ImageExt.Contains(ext)) return FileCategory.Image;
        if (DocExt.Contains(ext)) return FileCategory.Document;
        if (MusicExt.Contains(ext)) return FileCategory.Music;
        if (VideoExt.Contains(ext)) return FileCategory.Video;
        return FileCategory.Other;
    }



public static IReadOnlyCollection<string> ImageExtensions => ImageExt.ToArray();
public static IReadOnlyCollection<string> DocumentExtensions => DocExt.ToArray();
public static IReadOnlyCollection<string> MusicExtensions => MusicExt.ToArray();
public static IReadOnlyCollection<string> VideoExtensions => VideoExt.ToArray();

public static IReadOnlyCollection<string> AllKnownExtensions =>
    ImageExt.Concat(DocExt).Concat(MusicExt).Concat(VideoExt).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string NormalizeExt(string extension)
    {
        var ext = (extension ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ext)) return "";
        if (!ext.StartsWith(".")) ext = "." + ext;
        return ext.ToLowerInvariant();
    }
}
