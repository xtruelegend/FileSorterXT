using System.IO;

namespace FileSorterXT.Services;

/// <summary>
/// Local (non-OneDrive) default folders.
/// This intentionally avoids Windows Known Folder redirection to OneDrive by resolving under %USERPROFILE%.
/// If those folders don't exist, they are created.
/// </summary>
public static class KnownFolders
{
    private static string UserRoot => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string Pictures => Ensure(Path.Combine(UserRoot, "Pictures"));
    public static string Documents => Ensure(Path.Combine(UserRoot, "Documents"));
    public static string Music => Ensure(Path.Combine(UserRoot, "Music"));
    public static string Videos => Ensure(Path.Combine(UserRoot, "Videos"));

    private static string Ensure(string path)
    {
        try { Directory.CreateDirectory(path); } catch { }
        return path;
    }
}
