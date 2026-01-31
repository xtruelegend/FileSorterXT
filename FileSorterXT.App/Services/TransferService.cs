using System.IO;
using System.Security.Cryptography;

namespace FileSorterXT.Services;

public static class TransferService
{
    public record TransferProgress(double Percent, string Message);
    public record TransferResult(bool Canceled, int FilesProcessed, int Failed);

    public static string GetNonCollidingFilePath(string desiredPath)
    {
        if (!File.Exists(desiredPath)) return desiredPath;

        var dir = Path.GetDirectoryName(desiredPath) ?? "";
        var name = Path.GetFileNameWithoutExtension(desiredPath);
        var ext = Path.GetExtension(desiredPath);

        for (int i = 1; i < 1000; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }

        return Path.Combine(dir, $"{name} ({Guid.NewGuid():N}){ext}");
    }

    public static string GetNonCollidingFolderPath(string desiredPath)
    {
        if (!Directory.Exists(desiredPath)) return desiredPath;

        var basePath = desiredPath.TrimEnd(Path.DirectorySeparatorChar);
        var parent = Path.GetDirectoryName(basePath) ?? "";
        var name = Path.GetFileName(basePath);

        for (int i = 1; i < 1000; i++)
        {
            var candidate = Path.Combine(parent, $"{name} ({i})");
            if (!Directory.Exists(candidate)) return candidate;
        }

        return Path.Combine(parent, $"{name} ({Guid.NewGuid():N})");
    }

    public static async Task<TransferResult> TransferFolderAsync(
        string sourceFolder,
        string destFolder,
        bool copyMode,
        bool verify,
        Action<TransferProgress> report,
        CancellationToken token)
    {
        var files = Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories).ToList();

        long totalBytes = 0;
        foreach (var f in files)
        {
            try { totalBytes += new FileInfo(f).Length; } catch { }
        }

        int processed = 0;
        int failed = 0;
        long doneBytes = 0;

        try
        {
            foreach (var src in files)
            {
                token.ThrowIfCancellationRequested();

                var rel = Path.GetRelativePath(sourceFolder, src);
                var dest = Path.Combine(destFolder, rel);
                dest = GetNonCollidingFilePath(dest);

                var destDir = Path.GetDirectoryName(dest) ?? destFolder;

                try
                {
                    Directory.CreateDirectory(destDir);

                    if (copyMode)
                    {
                        File.Copy(src, dest, overwrite: false);

                        if (verify)
                        {
                            if (!VerifyFile(src, dest)) throw new Exception("Verify failed.");
                        }
                    }
                    else
                    {
                        // Same drive: move is fast but source disappears, so verify is limited
                        if (string.Equals(Path.GetPathRoot(src), Path.GetPathRoot(dest), StringComparison.OrdinalIgnoreCase))
                        {
                            long srcLen = 0;
                            try { srcLen = new FileInfo(src).Length; } catch { }

                            File.Move(src, dest);

                            if (verify && srcLen > 0)
                            {
                                long destLen = 0;
                                try { destLen = new FileInfo(dest).Length; } catch { }
                                if (destLen != srcLen) throw new Exception("Verify failed.");
                            }
                        }
                        else
                        {
                            // Different drive: copy, optionally verify using src, then delete src
                            File.Copy(src, dest, overwrite: false);

                            if (verify)
                            {
                                if (!VerifyFile(src, dest)) throw new Exception("Verify failed.");
                            }

                            File.Delete(src);
                        }
                    }

                    processed++;
                    try { doneBytes += new FileInfo(dest).Length; } catch { }

                    var pct = totalBytes <= 0
                        ? (processed * 100.0 / Math.Max(1, files.Count))
                        : (doneBytes * 100.0 / Math.Max(1, totalBytes));

                    report(new TransferProgress(pct, $"Transferring: {rel}"));
                }
                catch
                {
                    failed++;
                }

                await Task.Yield();
            }
        }
        catch (OperationCanceledException)
        {
            return new TransferResult(true, processed, failed);
        }

        if (!copyMode)
        {
            try { CleanupEmptyDirs(sourceFolder); } catch { }
        }

        return new TransferResult(false, processed, failed);
    }

    private static bool VerifyFile(string src, string dest)
    {
        try
        {
            if (!File.Exists(dest) || !File.Exists(src)) return false;

            var a = new FileInfo(src).Length;
            var b = new FileInfo(dest).Length;
            if (a != b) return false;

            // Hash on smaller files, size check on very large files
            if (a > 256L * 1024 * 1024) return true;

            using var sha = SHA256.Create();
            using var fs1 = File.OpenRead(src);
            using var fs2 = File.OpenRead(dest);
            var h1 = sha.ComputeHash(fs1);
            var h2 = sha.ComputeHash(fs2);
            return h1.SequenceEqual(h2);
        }
        catch
        {
            return false;
        }
    }

    private static void CleanupEmptyDirs(string root)
    {
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                                     .OrderByDescending(d => d.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }
    }
}
