using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.Sync;

/// <summary>
/// Turns Cloud Sync off locally and stops the orchestrator.
/// </summary>
public sealed class DisableCloudSyncUseCase(
    ISyncMetadataStore metadataStore,
    ICloudSyncOrchestrator orchestrator)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = await metadataStore.GetAsync().ConfigureAwait(false);
        metadata.SyncEnabled = false;
        await metadataStore.SaveAsync(metadata).ConfigureAwait(false);
        await orchestrator.StopAsync().ConfigureAwait(false);
    }
}
