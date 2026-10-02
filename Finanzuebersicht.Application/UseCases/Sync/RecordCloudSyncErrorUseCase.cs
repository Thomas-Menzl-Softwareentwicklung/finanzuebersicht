using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed class RecordCloudSyncErrorUseCase(ISyncMetadataStore metadataStore)
{
    public async Task ExecuteAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = await metadataStore.GetAsync().ConfigureAwait(false);
        metadata.LastError = message;
        await metadataStore.SaveAsync(metadata).ConfigureAwait(false);
    }
}
