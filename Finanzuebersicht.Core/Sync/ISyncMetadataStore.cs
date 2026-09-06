namespace Finanzuebersicht.Core.Sync;

public interface ISyncMetadataStore
{
    Task<SyncMetadata> GetAsync();
    Task SaveAsync(SyncMetadata meta);
}
