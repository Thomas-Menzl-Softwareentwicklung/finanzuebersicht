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
#endif
}
