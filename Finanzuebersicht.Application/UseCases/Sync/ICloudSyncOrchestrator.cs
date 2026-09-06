using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.Sync;

public interface ICloudSyncOrchestrator
{
    Task StartIfEnabledAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task NotifyLocalUpsertAsync(SyncEntityType type, string id, CancellationToken ct = default);
    Task NotifyLocalDeleteAsync(SyncEntityType type, string id, CancellationToken ct = default);
    Task SyncNowAsync(CancellationToken ct = default);
}
