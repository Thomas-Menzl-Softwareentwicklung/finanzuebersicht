namespace Finanzuebersicht.Core.Sync;

public interface ISyncTombstoneStore
{
    Task<IReadOnlyList<SyncTombstone>> GetAllAsync();
    Task UpsertAsync(SyncTombstone tombstone);
    Task RemoveAsync(SyncEntityType type, string id);
}
