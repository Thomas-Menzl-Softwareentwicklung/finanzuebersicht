namespace Finanzuebersicht.Core.Sync;

public sealed class SyncTombstone
{
    public SyncEntityType EntityType { get; set; }
    public string Id { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}
