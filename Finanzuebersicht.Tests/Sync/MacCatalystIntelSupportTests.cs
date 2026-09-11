namespace Finanzuebersicht.Tests.Sync;

/// <summary>
/// Mac App Store / TestFlight reject arm64-only Mac Catalyst packages, and
/// <c>UIRequiredDeviceCapabilities → arm64</c> hides Intel Macs even when the
/// binary is universal. iOS still requires arm64.
/// Universal Release AOTs all assemblies except the entry assembly (interpreted)
/// so Apple Silicon can start and Intel is not forced onto a full interpreter.
/// </summary>
public class MacCatalystIntelSupportTests
{
    [Fact]
    public void MacCatalystInfoPlist_DoesNotRequireArm64()
    {
        var plist = File.ReadAllText(FindRepoFile("Finanzuebersicht/Platforms/MacCatalyst/Info.plist"));
        Assert.DoesNotContain("<key>UIRequiredDeviceCapabilities</key>", plist, StringComparison.Ordinal);
        Assert.DoesNotContain("<string>arm64</string>", plist, StringComparison.Ordinal);
    }

    [Fact]
    public void IosInfoPlist_StillRequiresArm64()
    {
        var plist = File.ReadAllText(FindRepoFile("Finanzuebersicht/Platforms/iOS/Info.plist"));
        Assert.Contains("<key>UIRequiredDeviceCapabilities</key>", plist, StringComparison.Ordinal);
        Assert.Contains("<string>arm64</string>", plist, StringComparison.Ordinal);
    }

    [Fact]
    public void MacCatalyst_InterpretsEntryAssemblyOnRelease()
    {
        var csproj = File.ReadAllText(FindRepoFile("Finanzuebersicht/Finanzuebersicht.csproj"));
        Assert.Contains(
            "<UseInterpreter Condition=\"'$(Configuration)' == 'Debug'\">true</UseInterpreter>",
            csproj,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "<UseInterpreter Condition=\"$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'\">true</UseInterpreter>",
            csproj,
            StringComparison.Ordinal);
        Assert.Contains(
            "Condition=\"$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst' AND '$(Configuration)' == 'Release'\"",
            csproj,
            StringComparison.Ordinal);
        Assert.Contains(
            "<MtouchInterpreter>-all,Finanzuebersicht</MtouchInterpreter>",
            csproj,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CloudKitBridgeScript_BuildsFatMacCatalystLibrary()
    {
        var script = File.ReadAllText(FindRepoFile("Finanzuebersicht/Platforms/iOS/Native/build-cloudkit-sync-bridge.sh"));
        Assert.Contains("x86_64-apple-ios15.0-macabi", script, StringComparison.Ordinal);
        Assert.Contains("arm64-apple-ios15.0-macabi", script, StringComparison.Ordinal);
        Assert.Contains("lipo -create", script, StringComparison.Ordinal);
        Assert.Contains("Release-maccatalyst", script, StringComparison.Ordinal);
        Assert.Contains("$FAT_DIR/libCloudKitSyncBridge.a", script, StringComparison.Ordinal);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"{relativePath} not found from test output directory.");
    }
}
