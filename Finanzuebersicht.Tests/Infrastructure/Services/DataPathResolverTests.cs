using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;
using Finanzuebersicht.Infrastructure.Services;
using Finanzuebersicht.Tests.TestHelpers;

namespace Finanzuebersicht.Tests.Infrastructure.Services;

[Collection(nameof(ScreenshotDemoLaunchOptionsCollection))]
public class DataPathResolverTests
{
    [Fact]
    public void ResolveDataDir_UsesActiveDataPath_WhenNoPending()
    {
        using var scope = new SettingsScope(
            nameof(DataPathResolverTests),
            (SettingsKeys.DataPath, "/Users/test/active"));

        var result = DataPathResolver.ResolveDataDir(scope.Settings);

        Assert.Equal("/Users/test/active", result);
        Assert.False(scope.Settings.Contains(SettingsKeys.DataPathPending));
    }

    [Fact]
    public void ResolveDataDir_AppliesPendingPath_AndRemovesPendingKey()
    {
        using var scope = new SettingsScope(
            nameof(DataPathResolverTests),
            (SettingsKeys.DataPath, "/Users/test/old"),
            (SettingsKeys.DataPathPending, "/Users/test/new"));

        var result = DataPathResolver.ResolveDataDir(scope.Settings);

        Assert.Equal("/Users/test/new", result);
        Assert.Equal("/Users/test/new", scope.Settings.Get(SettingsKeys.DataPath));
        Assert.False(scope.Settings.Contains(SettingsKeys.DataPathPending));
    }

    [Fact]
    public void ResolveDataDir_AppliesPendingReset_ToDefaultDirectory()
    {
        using var scope = new SettingsScope(
            nameof(DataPathResolverTests),
            (SettingsKeys.DataPath, "/Users/test/custom"),
            (SettingsKeys.DataPathPending, string.Empty));

        var result = DataPathResolver.ResolveDataDir(scope.Settings);

        Assert.Equal(AppPaths.GetDefaultDataDir(), result);
        Assert.Equal(string.Empty, scope.Settings.Get(SettingsKeys.DataPath));
        Assert.False(scope.Settings.Contains(SettingsKeys.DataPathPending));
    }

    [Fact]
    public void ApplyPendingDataPath_DoesNothing_WhenPendingMissing()
    {
        using var scope = new SettingsScope(
            nameof(DataPathResolverTests),
            (SettingsKeys.DataPath, "/Users/test/active"));

        DataPathResolver.ApplyPendingDataPath(scope.Settings);

        Assert.Equal("/Users/test/active", scope.Settings.Get(SettingsKeys.DataPath));
        Assert.False(scope.Settings.Contains(SettingsKeys.DataPathPending));
    }

#if DEBUG
    [Fact]
    public void ResolveDataDir_ReturnsIsolatedPath_WhenScreenshotDemoRequested_WithoutMutatingSettings()
    {
        ScreenshotDemoLaunchOptions.CommandLineArgsOverride = () =>
            ["Finanzuebersicht", ScreenshotDemoLaunchOptions.LaunchArgument];

        try
        {
            using var scope = new SettingsScope(
                nameof(DataPathResolverTests),
                (SettingsKeys.DataPath, "/Users/test/active"));

            var result = DataPathResolver.ResolveDataDir(scope.Settings);

            Assert.Equal(ScreenshotDemoLaunchOptions.GetIsolatedDataPath(), result);
            Assert.Equal("/Users/test/active", scope.Settings.Get(SettingsKeys.DataPath));
            Assert.False(scope.Settings.Contains(SettingsKeys.DataPathPending));
        }
        finally
        {
            ScreenshotDemoLaunchOptions.CommandLineArgsOverride = null;
        }
    }
#endif
}
