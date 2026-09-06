namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed class EnableCloudSyncResult
{
    public EnableCloudSyncStatus Status { get; init; }
    public string? MessageKey { get; init; }
}

public enum EnableCloudSyncStatus
{
    Enabled,
    BlockedBothHaveData,
    BlockedNoEntitlement,
    BlockedUnsupported,
    BlockedNoICloud
}
