using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;
using Finanzuebersicht.Tests.TestHelpers;

namespace Finanzuebersicht.Tests.Core.Services.ScreenshotDemo;

[Collection(nameof(ScreenshotDemoLaunchOptionsCollection))]
public class ScreenshotDemoLaunchOptionsTests
{
    [Fact]
    public void GetIsolatedDataPath_ReturnsScreenshotDemoFolderUnderDefaultDataDir()
    {
        var path = ScreenshotDemoLaunchOptions.GetIsolatedDataPath();

        Assert.Equal(Path.Combine(AppPaths.GetDefaultDataDir(), "screenshot-demo"), path);
        Assert.True(Directory.Exists(path));
    }

#if DEBUG
    [Theory]
    [InlineData(true, "accounts", "accounts")]
    [InlineData(true, "import", "import")]
    [InlineData(true, "quick-expense", "quick-expense")]
    [InlineData(true, "unknown", null)]
    [InlineData(false, "import", null)]
    public void Scene_IsWhitelistedAndRequiresDemoSession(bool demo, string scene, string? expected)
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () => demo
            ? ["app", ScreenshotDemoLaunchOptions.LaunchArgument, "--screenshot-scene", scene]
            : ["app", "--screenshot-scene", scene];
        try { Assert.Equal(expected, ScreenshotDemoLaunchOptions.TryGetRequestedScene()); }
        finally { ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null; }
    }

    [Fact]
    public void IsRequested_ReturnsTrue_WhenLaunchArgumentPresent()
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () =>
            ["Finanzuebersicht", ScreenshotDemoLaunchOptions.LaunchArgument];

        try
        {
            Assert.True(ScreenshotDemoLaunchOptions.IsRequested());
        }
        finally
        {
            ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        }
    }

    [Fact]
    public void IsRequested_ReturnsFalse_WhenLaunchArgumentMissing()
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () => ["Finanzuebersicht"];

        try
        {
            Assert.False(ScreenshotDemoLaunchOptions.IsRequested());
        }
        finally
        {
            ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        }
    }

    [Fact]
    public void IsRequested_ReturnsTrue_WhenLaunchArgumentInPlatformArgsProvider()
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        ScreenshotDemoLaunchOptions.PlatformArgsProvider = () =>
            ["Finanzuebersicht", ScreenshotDemoLaunchOptions.LaunchArgument];

        try
        {
            Assert.True(ScreenshotDemoLaunchOptions.IsRequested());
        }
        finally
        {
            ScreenshotDemoLaunchOptions.PlatformArgsProvider = null;
        }
    }

    [Fact]
    public void IsRequested_IgnoresPlatformArgsProvider_WhenCommandLineArgsOverrideSet()
    {
        ScreenshotDemoLaunchOptions.PlatformArgsProvider = () =>
            ["Finanzuebersicht", ScreenshotDemoLaunchOptions.LaunchArgument];
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () => ["Finanzuebersicht"];

        try
        {
            Assert.False(ScreenshotDemoLaunchOptions.IsRequested());
        }
        finally
        {
            ScreenshotDemoLaunchOptions.PlatformArgsProvider = null;
            ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        }
    }

    [Fact]
    public void TryGetRequestedCulture_ReturnsEnUs_WhenAppleLanguagesEn()
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () =>
            ["Finanzuebersicht", ScreenshotDemoLaunchOptions.LaunchArgument, "-AppleLanguages", "(en-US)"];

        try
        {
            var culture = ScreenshotDemoLaunchOptions.TryGetRequestedCulture();
            Assert.NotNull(culture);
            Assert.Equal("en-US", culture.Name);
        }
        finally
        {
            ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        }
    }

    [Fact]
    public void TryGetRequestedCulture_ReturnsDeDe_WhenAppleLanguagesDe()
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () =>
            ["Finanzuebersicht", "-AppleLanguages", "(de-DE)"];

        try
        {
            var culture = ScreenshotDemoLaunchOptions.TryGetRequestedCulture();
            Assert.NotNull(culture);
            Assert.Equal("de-DE", culture.Name);
        }
        finally
        {
            ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        }
    }

    [Fact]
    public void TryGetRequestedCulture_ReturnsNull_WhenAppleLanguagesMissing()
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () =>
            ["Finanzuebersicht", ScreenshotDemoLaunchOptions.LaunchArgument];

        try
        {
            Assert.Null(ScreenshotDemoLaunchOptions.TryGetRequestedCulture());
        }
        finally
        {
            ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        }
    }
#endif
}
