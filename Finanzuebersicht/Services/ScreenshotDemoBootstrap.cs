using Finanzuebersicht.Application.UseCases.ScreenshotDemo;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;

namespace Finanzuebersicht.Services;

/// <summary>
/// DEBUG-only bootstrap for App Store screenshot runs (<c>--screenshot-demo</c>).
/// Applies an isolated <see cref="SettingsKeys.DataPath"/> before data stores resolve, then seeds demo data after init.
/// Snapfile / Simulator must pass <see cref="LaunchArgument"/> (see docs/superpowers/plans/2026-08-12-screenshot-automation.md).
/// </summary>
public static class ScreenshotDemoBootstrap
{
    public const string LaunchArgument = ScreenshotDemoLaunchOptions.LaunchArgument;

    public static bool IsRequested() => ScreenshotDemoLaunchOptions.IsRequested();

    public static string GetIsolatedDataPath() => ScreenshotDemoLaunchOptions.GetIsolatedDataPath();

    /// <summary>
    /// When demo mode is active, sets isolated DataPath and screenshot-friendly settings.
    /// Call after <c>MauiApp</c> build and before <see cref="App"/> construction.
    /// </summary>
    public static bool TryApplyAsync(ISettingsService settings)
    {
#if DEBUG && !APP_DISTRIBUTION_STORE
        if (!IsRequested())
            return false;

        settings.Set(SettingsKeys.DataPath, GetIsolatedDataPath());
        settings.Remove(SettingsKeys.LanguageCode);
        settings.Set(SettingsKeys.OnboardingCompleted, "true");
        settings.Set(SettingsKeys.Theme, ThemeValues.Light);
        return true;
#else
        _ = settings;
        return false;
#endif
    }

    /// <summary>Seeds deterministic demo data after <see cref="InitializationService.InitializeAsync"/>.</summary>
    public static async Task TrySeedAsync(
        SeedScreenshotDemoDataUseCase seedUseCase,
        CancellationToken cancellationToken = default)
    {
#if DEBUG && !APP_DISTRIBUTION_STORE
        if (!IsRequested())
            return;

        await seedUseCase.ExecuteAsync(cancellationToken);
#else
        _ = seedUseCase;
        await Task.CompletedTask;
#endif
    }
}
