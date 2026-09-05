namespace Finanzuebersicht.Core.Sync;

public interface ICloudSyncTransport
{
    bool IsSupported { get; } // false on Null / Windows / OS < 17
    Task<CloudSyncAccountStatus> GetAccountStatusAsync(CancellationToken ct = default);
    Task<bool> IsZoneEmptyAsync(CancellationToken ct = default);
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task EnqueueUpsertAsync(CloudSyncRecordDto record, CancellationToken ct = default);
    Task EnqueueDeleteAsync(SyncEntityType type, string id, DateTime deletedAt, CancellationToken ct = default);
    Task FetchChangesAsync(CancellationToken ct = default);
    Task SendChangesAsync(CancellationToken ct = default);
    event EventHandler<IReadOnlyList<CloudSyncRecordDto>>? RecordsChanged;
}
