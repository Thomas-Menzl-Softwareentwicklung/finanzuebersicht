using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed class GetCloudSyncStatusUseCase(ISyncMetadataStore metadataStore)
{
    public async Task<CloudSyncStatusSnapshot> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = await metadataStore.GetAsync().ConfigureAwait(false);
        return new CloudSyncStatusSnapshot(metadata.SyncEnabled, metadata.LastError, metadata.LastSyncUtc);
    }
}
