namespace Finanzuebersicht.Core.Services.ScreenshotDemo;

/// <summary>
/// Launch-argument detection and isolated data path for screenshot automation (DEBUG builds).
/// </summary>
public static class ScreenshotDemoLaunchOptions
{
    public const string LaunchArgument = "--screenshot-demo";

    /// <summary>Test hook for command-line args (Finanzuebersicht.Tests).</summary>
    internal static Func<IEnumerable<string>>? CommandLineArgsOverride { get; set; }

    public static bool IsRequested()
    {
#if !DEBUG
        return false;
#else
        var args = CommandLineArgsOverride?.Invoke() ?? Environment.GetCommandLineArgs();
        return args.Any(a => string.Equals(a, LaunchArgument, StringComparison.Ordinal));
#endif
    }

    public static string GetIsolatedDataPath()
    {
        var root = Path.Combine(AppPaths.GetDefaultDataDir(), "screenshot-demo");
        Directory.CreateDirectory(root);
        return root;
    }
}
