using System.Globalization;

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

    /// <summary>
    /// Culture from <c>-AppleLanguages</c> (fastlane snapshot / Mac screenshot launches).
    /// Null when the flag is absent — callers fall back to <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public static CultureInfo? TryGetRequestedCulture()
    {
#if !DEBUG
        return null;
#else
        var args = GetEffectiveArgs().ToList();
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (!string.Equals(args[i], "-AppleLanguages", StringComparison.Ordinal))
                continue;

            var token = args[i + 1].Trim().Trim('"', '\'');
            token = token.TrimStart('(').TrimEnd(')').Split(',')[0].Trim().Trim('"');
            if (token.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return new CultureInfo("en-US");
            if (token.StartsWith("de", StringComparison.OrdinalIgnoreCase))
                return new CultureInfo("de-DE");
        }

        return null;
#endif
    }

    public static string GetIsolatedDataPath()
    {
        var root = Path.Combine(AppPaths.GetDefaultDataDir(), "screenshot-demo");
        Directory.CreateDirectory(root);
        return root;
    }
}
