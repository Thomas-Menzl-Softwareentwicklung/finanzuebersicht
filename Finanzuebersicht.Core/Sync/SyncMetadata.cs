namespace Finanzuebersicht.Core.Sync;

public sealed class SyncMetadata
{
    public bool SyncEnabled { get; set; }
    public DateTime? LastSyncUtc { get; set; }
    public int SchemaVersionSeen { get; set; }
    public string? LastError { get; set; }
}
