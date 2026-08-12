using Finanzuebersicht.Core.Services.ScreenshotDemo;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// Resolves the active data directory from settings, applying any pending path change
/// that was deferred until the next app start (store singletons bind DataDir at creation).
/// </summary>
public static class DataPathResolver
{
    /// <summary>
    /// Applies <see cref="SettingsKeys.DataPathPending"/> onto <see cref="SettingsKeys.DataPath"/>
    /// if present, then returns the effective data directory.
    /// </summary>
    public static string ResolveDataDir(ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (ScreenshotDemoLaunchOptions.IsRequested())
            return ScreenshotDemoLaunchOptions.GetIsolatedDataPath();

        ApplyPendingDataPath(settings);

        var customPath = settings.Get(SettingsKeys.DataPath, "");
        return string.IsNullOrWhiteSpace(customPath)
            ? AppPaths.GetDefaultDataDir()
            : customPath;
    }

    /// <summary>
    /// If a pending data path exists, commits it to <see cref="SettingsKeys.DataPath"/>
    /// (empty string = default) and removes the pending key.
    /// </summary>
    public static void ApplyPendingDataPath(ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Contains(SettingsKeys.DataPathPending))
            return;

        var pending = settings.Get(SettingsKeys.DataPathPending, "");
        settings.Set(SettingsKeys.DataPath, pending);
        settings.Remove(SettingsKeys.DataPathPending);
    }
}
