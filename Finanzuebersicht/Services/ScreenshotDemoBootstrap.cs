using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Finanzuebersicht.Application.UseCases.ScreenshotDemo;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;

namespace Finanzuebersicht.Services;

/// <summary>
/// DEBUG-only bootstrap for App Store screenshot runs (<c>--screenshot-demo</c>).
/// Applies session-only screenshot settings (isolated DataPath, theme, language, onboarding skip)
/// without persisting to default settings.json.
/// Snapfile / Simulator must pass <see cref="LaunchArgument"/> (see docs/superpowers/plans/2026-08-12-screenshot-automation.md).
/// </summary>
public static class ScreenshotDemoBootstrap
{
    public const string LaunchArgument = ScreenshotDemoLaunchOptions.LaunchArgument;

    public static bool IsRequested() => ScreenshotDemoLaunchOptions.IsRequested();

    public static bool IsActive() => ScreenshotDemoLaunchOptions.IsActive();

    public static string GetIsolatedDataPath() => ScreenshotDemoLaunchOptions.GetIsolatedDataPath();

    /// <summary>
    /// When demo mode is active, reports that session-only screenshot settings apply.
    /// DataPath, theme, language, and onboarding are handled without persisting to default settings.
    /// Call after <c>MauiApp</c> build and before <see cref="App"/> construction.
    /// </summary>
    public static bool TryApplyAsync(ISettingsService settings)
    {
#if DEBUG && !APP_DISTRIBUTION_STORE
        _ = settings;
        return IsRequested();
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
    /// <summary>Open real screens after seeding, without touching the user's normal session.</summary>
    public static async Task TryOpenSceneAsync()
    {
#if DEBUG && !APP_DISTRIBUTION_STORE
        var scene = ScreenshotDemoLaunchOptions.TryGetRequestedScene();
        if (scene is null) return;
        var shell = Shell.Current;
        var services = shell.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("Screenshot scene requires an initialized Shell.");
        var navigation = services.GetRequiredService<INavigationService>();
        var route = scene switch
        {
            "transactions" or "import" => Routes.TransactionsTab,
            "accounts" or "management" => Routes.CategoriesTab,
            "recurring" => Routes.RecurringTransactionsTab,
            "savings" => Routes.SparZieleTab,
            _ => Routes.DashboardTab
        };
        async Task<Page> WaitForPageAsync(string name)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (shell.CurrentPage is { } current && current.GetType().Name == name)
                    return current;
                await Task.Delay(100);
            }
            throw new TimeoutException($"Screenshot scene {scene}: page {name} did not become ready.");
        }

        await navigation.GoToAsync(route);
        var page = await WaitForPageAsync(route.TrimStart('/'));
        if (scene == "accounts" && page.BindingContext is CategoriesViewModel categories)
            categories.SelectedSectionIndex = 1;
        if (scene == "import")
        {
            await services.GetRequiredService<TransactionImportCoordinator>().ImportCsvAsync(null);
            page = await WaitForPageAsync(Routes.ImportPreview);
        }
        if (scene == "settings")
        {
            await navigation.GoToAsync(Routes.Settings);
            page = await WaitForPageAsync(Routes.Settings);
        }
        if (scene == "quick-expense")
        {
            await navigation.GoToAsync(Routes.QuickExpenseCapture, new Dictionary<string, object>
            {
                [NavigationQueryKeys.Amount] = "8.90",
                [NavigationQueryKeys.Title] = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en"
                    ? "Coffee and cake" : "Kaffee und Kuchen"
            });
            page = await WaitForPageAsync(Routes.QuickExpenseCapture);
        }
        if (page.BindingContext is IAutoLoadViewModel vm &&
            vm.AutoLoadCommand is IAsyncRelayCommand command)
        {
            if (command.ExecutionTask is { IsCompleted: false } running) await running;
            else if (vm.ShouldAutoLoad) await command.ExecuteAsync(null);
        }
        // The Mac capture script waits for this marker instead of capturing a half-loaded screen.
        var language = CultureInfo.CurrentUICulture.Name;
        await File.WriteAllTextAsync(Path.Combine(GetIsolatedDataPath(), $"capture-ready-{language}-{scene}"), scene);
#endif
    }

}
