using Finanzuebersicht.Core.Sync;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

public class SyncTombstoneStore : JsonDataStoreBase, ISyncTombstoneStore
{
    private string TombstonesFile => Path.Combine(DataDir, DataFileNames.SyncTombstones);

    public SyncTombstoneStore(string dataDir, ILogger<SyncTombstoneStore>? logger = null)
        : base(dataDir, logger)
    {
    }

    public async Task<IReadOnlyList<SyncTombstone>> GetAllAsync()
    {
        await StoreLock.WaitAsync();
        try
        {
            return await LoadAsync<SyncTombstone>(TombstonesFile);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task UpsertAsync(SyncTombstone tombstone)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<SyncTombstone>(TombstonesFile);
            var idx = items.FindIndex(t => t.EntityType == tombstone.EntityType && t.Id == tombstone.Id);
            if (idx >= 0)
                items[idx] = tombstone;
            else
                items.Add(tombstone);
            await SaveAsync(TombstonesFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task RemoveAsync(SyncEntityType type, string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<SyncTombstone>(TombstonesFile);
            items.RemoveAll(t => t.EntityType == type && t.Id == id);
            await SaveAsync(TombstonesFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }
}
