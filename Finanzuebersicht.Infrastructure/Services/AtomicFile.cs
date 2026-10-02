namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// Replace-atomic text file writes (temp in same directory → replace/move).
/// </summary>
public static class AtomicFile
{
    public static async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(contents);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, contents, cancellationToken).ConfigureAwait(false);
            ReplaceOrMove(tempPath, path);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    public static void WriteAllText(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(contents);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(tempPath, contents);
            ReplaceOrMove(tempPath, path);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private static void ReplaceOrMove(string sourcePath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            // destinationBackupFileName: null — no backup of the replaced file
            File.Replace(sourcePath, destinationPath, null);
            return;
        }

        File.Move(sourcePath, destinationPath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup of orphaned temp files.
        }
    }
}
