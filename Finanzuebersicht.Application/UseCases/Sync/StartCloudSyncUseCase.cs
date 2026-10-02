namespace Finanzuebersicht.Application.UseCases.Sync;

/// <summary>
/// Starts the orchestrator when sync is enabled and runs one sync pass.
/// </summary>
public sealed class StartCloudSyncUseCase(ICloudSyncOrchestrator orchestrator)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await orchestrator.StartIfEnabledAsync(cancellationToken).ConfigureAwait(false);
        await orchestrator.SyncNowAsync(cancellationToken).ConfigureAwait(false);
    }
}
