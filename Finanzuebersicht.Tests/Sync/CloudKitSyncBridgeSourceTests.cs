namespace Finanzuebersicht.Tests.Sync;

/// <summary>
/// First-enable on TestFlight queries a custom zone that does not exist yet. Production
/// often wraps that as <c>CKError.partialFailure</c> instead of <c>zoneNotFound</c>,
/// which the bridge used to surface as native status 4.
/// </summary>
public class CloudKitSyncBridgeSourceTests
{
    [Fact]
    public void IsZoneEmpty_TreatsMissingZoneAsEmpty()
    {
        var source = File.ReadAllText(FindBridgeSource());

        Assert.Contains("allRecordZones", source, StringComparison.Ordinal);
        Assert.Contains("isAbsentZone", source, StringComparison.Ordinal);
        Assert.Contains(".partialFailure", source, StringComparison.Ordinal);
        Assert.Contains("partialErrorsByItemID", source, StringComparison.Ordinal);
        Assert.Contains("pendingDatabaseChanges: [.saveZone", source, StringComparison.Ordinal);
        Assert.Contains("func resetEngineState()", source, StringComparison.Ordinal);
        Assert.Contains("finanzuebersicht_ck_reset_engine_state", source, StringComparison.Ordinal);
    }

    private static string FindBridgeSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "Finanzuebersicht",
                "Platforms",
                "iOS",
                "Native",
                "CloudKitSyncBridge.swift");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("CloudKitSyncBridge.swift not found from test output directory.");
    }
}
