using System.IO.Compression;

namespace AethertekUI.Dalamud;

/// <summary>One manually requested snapshot of a host log, without blocking its writer.</summary>
public static class SupportLogArchive
{
    public const long DalamudLogLimit = 100L * 1024 * 1024;
    public static bool IsCapped(long length) => length >= DalamudLogLimit;

    public static string Create(string logPath, string outputDirectory)
    {
        using var source = new FileStream(logPath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        var remaining = source.Length;
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"dalamud-{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}.zip");
        var created = false;
        try
        {
            using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            created = true;
            using var archive = new ZipArchive(output, ZipArchiveMode.Create);
            using var entry = archive.CreateEntry("dalamud.log", CompressionLevel.Fastest).Open();
            var buffer = new byte[64 * 1024];
            // Copy only the length at open: an active writer must not extend this operation.
            while (remaining > 0)
            {
                var read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read == 0) throw new IOException("The log changed before its snapshot could be completed.");
                entry.Write(buffer, 0, read);
                remaining -= read;
            }
            return outputPath;
        }
        catch
        {
            if (created) File.Delete(outputPath);
            throw;
        }
    }
}
