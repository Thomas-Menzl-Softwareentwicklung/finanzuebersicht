namespace Finanzuebersicht.Core.Services.ScreenshotDemo;

/// <summary>
/// Launch-argument detection and isolated data path for screenshot automation (DEBUG builds).
/// </summary>
public static class ScreenshotDemoLaunchOptions
{
    public const string LaunchArgument = "--screenshot-demo";

    /// <summary>Test hook for command-line args (Finanzuebersicht.Tests).</summary>
    internal static Func<IEnumerable<string>>? CommandLineArgsOverride { get; set; }

    /// <summary>
    /// iOS / Mac Catalyst: XCTest <c>launchArguments</c> appear in <c>NSProcessInfo.ProcessInfo.Arguments</c>,
    /// not <see cref="Environment.GetCommandLineArgs"/>. Registered from <c>MauiProgram</c> at startup.
    /// </summary>
    internal static Func<IEnumerable<string>>? PlatformArgsProvider { get; set; }

    public static bool IsRequested()
    {
#if !DEBUG
        return false;
#else
        return GetEffectiveArgs().Any(a => string.Equals(a, LaunchArgument, StringComparison.Ordinal));
#endif
    }

    /// <summary>
    /// True when screenshot demo mode is active for this build (DEBUG, non-Store distribution).
    /// Session hooks (DataPath, localization, onboarding, widget isolation) use this instead of
    /// <see cref="IsRequested"/> so Store Debug builds do not apply demo side effects.
    /// </summary>
    public static bool IsActive()
    {
#if DEBUG && !APP_DISTRIBUTION_STORE
        return IsRequested();
#else
        return false;
#endif
    }

#if DEBUG
    private static IEnumerable<string> GetEffectiveArgs()
    {
        if (CommandLineArgsOverride is not null)
            return CommandLineArgsOverride();

        var env = Environment.GetCommandLineArgs();
        var platform = PlatformArgsProvider?.Invoke() ?? Array.Empty<string>();
        return env.Concat(platform);
    }
#endif

    public static string GetIsolatedDataPath()
    {
        var root = Path.Combine(AppPaths.GetDefaultDataDir(), "screenshot-demo");
        Directory.CreateDirectory(root);
        return root;
    }
}
