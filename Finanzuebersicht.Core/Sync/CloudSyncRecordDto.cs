namespace Finanzuebersicht.Core.Sync;

public sealed class CloudSyncRecordDto
{
    public SyncEntityType EntityType { get; init; }
    public string Id { get; init; } = "";
    public DateTime? UpdatedAt { get; init; }
    public string? PayloadJson { get; init; } // null if tombstone
    public bool IsTombstone { get; init; }
    public DateTime? DeletedAt { get; init; }
}
