using Finanzuebersicht.Core.Services.ScreenshotDemo;

namespace Finanzuebersicht.Tests.Core.Services.ScreenshotDemo;

public class ScreenshotDemoLaunchOptionsTests
{
    [Fact]
    public void GetIsolatedDataPath_ReturnsScreenshotDemoFolderUnderLocalAppData()
    {
        var path = ScreenshotDemoLaunchOptions.GetIsolatedDataPath();

        Assert.Contains("Finanzuebersicht", path);
        Assert.EndsWith("screenshot-demo", path);
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
#endif
}
