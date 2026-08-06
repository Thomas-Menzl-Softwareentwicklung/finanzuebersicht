namespace Finanzuebersicht.Services;

/// <summary>Append-only crash/diagnostic log under ~/Library/Logs/Finanzuebersicht/.</summary>
internal static class CrashLog
{
    private static readonly object Gate = new();

    internal static string LogDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "Logs", "Finanzuebersicht");

    internal static string LogPath => Path.Combine(LogDirectory, "crash.log");

    internal static void Write(string message, Exception? ex = null)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            if (ex is not null)
                line += Environment.NewLine + ex;

            lock (Gate)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine + Environment.NewLine);
            }
        }
        catch
        {
            // Never throw from the crash logger.
        }
    }
}
