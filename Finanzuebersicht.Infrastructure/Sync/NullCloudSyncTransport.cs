using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Infrastructure.Sync;

public sealed class NullCloudSyncTransport : ICloudSyncTransport
{
    public bool IsSupported => false;

    public event EventHandler<IReadOnlyList<CloudSyncRecordDto>>? RecordsChanged
    {
        add { }
        remove { }
    }

    public Task<CloudSyncAccountStatus> GetAccountStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(CloudSyncAccountStatus.NoAccount);

    public Task<bool> IsZoneEmptyAsync(CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task StartAsync(CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task EnqueueUpsertAsync(CloudSyncRecordDto record, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task EnqueueDeleteAsync(SyncEntityType type, string id, DateTime deletedAt, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task FetchChangesAsync(CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendChangesAsync(CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task ResetEngineStateAsync(CancellationToken ct = default) =>
        Task.CompletedTask;
}
