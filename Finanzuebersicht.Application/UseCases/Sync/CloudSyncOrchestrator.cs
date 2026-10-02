using System.Collections.Concurrent;
using System.Text.Json;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed partial class CloudSyncOrchestrator(
    ICloudSyncTransport transport,
    ISyncMetadataStore metadataStore,
    ISyncTombstoneStore tombstoneStore,
    IAccountRepository accountRepository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringTransactionRepository,
    ISparZielRepository sparZielRepository,
    ILicenseService licenseService) : ICloudSyncOrchestrator
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(1500);

    private readonly ConcurrentDictionary<(SyncEntityType Type, string Id), PendingUpsert> _pendingUpserts = new();
    private EventHandler<IReadOnlyList<CloudSyncRecordDto>>? _recordsChangedHandler;
    private bool _subscribed;
    private Task _lastApplyTask = Task.CompletedTask;

    public async Task StartIfEnabledAsync(CancellationToken ct = default)
    {
        if (!await IsSyncEnabledAsync())
        {
            return;
        }

        var metadata = await metadataStore.GetAsync();

        if (!_subscribed)
        {
            _recordsChangedHandler = OnRecordsChanged;
            transport.RecordsChanged += _recordsChangedHandler;
            _subscribed = true;
        }

        await transport.StartAsync(ct);
        await EnqueueDirtyEntitiesAsync(metadata, ct);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_subscribed && _recordsChangedHandler is not null)
        {
            transport.RecordsChanged -= _recordsChangedHandler;
            _subscribed = false;
            _recordsChangedHandler = null;
        }

        await FlushPendingUpsertsAsync();
        await TrySendChangesAsync(ct);
        await transport.StopAsync(ct);
    }

    public async Task NotifyLocalUpsertAsync(SyncEntityType type, string id, CancellationToken ct = default)
    {
        if (!await IsSyncEnabledAsync())
        {
            return;
        }

        var record = await BuildLocalUpsertRecordAsync(type, id, ct);
        if (record is null)
        {
            return;
        }

        ScheduleDebouncedUpsert((type, id), record);
    }

    public async Task NotifyLocalDeleteAsync(SyncEntityType type, string id, CancellationToken ct = default)
    {
        if (!await IsSyncEnabledAsync())
        {
            return;
        }

        var deletedAt = DateTime.UtcNow;
        await tombstoneStore.UpsertAsync(new SyncTombstone
        {
            EntityType = type,
            Id = id,
            DeletedAt = deletedAt
        });
        await transport.EnqueueDeleteAsync(type, id, deletedAt, ct);
    }

    public async Task SyncNowAsync(CancellationToken ct = default)
    {
        if (!await IsSyncEnabledAsync())
        {
            return;
        }

        try
        {
            await FlushPendingUpsertsAsync();

            await transport.FetchChangesAsync(ct);
            await transport.SendChangesAsync(ct);

            var metadata = await metadataStore.GetAsync();
            metadata.LastSyncUtc = DateTime.UtcNow;
            metadata.LastError = null;
            await metadataStore.SaveAsync(metadata);
        }
        catch (Exception ex)
        {
            await PersistLastErrorAsync(ex.Message);
        }
    }

    internal async Task FlushPendingForTestsAsync()
    {
        await FlushPendingUpsertsAsync();
        await TrySendChangesAsync();
    }

    internal Task WaitForLastApplyForTestsAsync() => _lastApplyTask;

    private async Task<bool> IsSyncEnabledAsync()
    {
        var metadata = await metadataStore.GetAsync();
        return metadata.SyncEnabled
            && licenseService.CanUseCloudSync
            && metadata.SchemaVersionSeen <= CloudSyncSchema.CurrentVersion;
    }

    private async Task TrySendChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await transport.SendChangesAsync(ct);
        }
        catch (Exception ex)
        {
            await PersistLastErrorAsync(ex.Message);
        }
    }

    private async Task PersistLastErrorAsync(string message)
    {
        var metadata = await metadataStore.GetAsync();
        metadata.LastError = message;
        await metadataStore.SaveAsync(metadata);
    }
}
