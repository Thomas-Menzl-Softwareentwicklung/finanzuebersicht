using System.Collections.Concurrent;
using System.Text.Json;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed class CloudSyncOrchestrator(
    ICloudSyncTransport transport,
    ISyncMetadataStore metadataStore,
    ISyncTombstoneStore tombstoneStore,
    IAccountRepository accountRepository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringTransactionRepository,
    ISparZielRepository sparZielRepository) : ICloudSyncOrchestrator
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
        var metadata = await metadataStore.GetAsync();
        if (!metadata.SyncEnabled)
        {
            return;
        }

        if (!_subscribed)
        {
            _recordsChangedHandler = OnRecordsChanged;
            transport.RecordsChanged += _recordsChangedHandler;
            _subscribed = true;
        }

        await transport.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_subscribed && _recordsChangedHandler is not null)
        {
            transport.RecordsChanged -= _recordsChangedHandler;
            _subscribed = false;
            _recordsChangedHandler = null;
        }

        CancelPendingUpserts();

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

        await FlushPendingUpsertsAsync();

        await transport.FetchChangesAsync(ct);
        await transport.SendChangesAsync(ct);

        var metadata = await metadataStore.GetAsync();
        metadata.LastSyncUtc = DateTime.UtcNow;
        await metadataStore.SaveAsync(metadata);
    }

    internal async Task FlushPendingForTestsAsync() => await FlushPendingUpsertsAsync();

    private async Task FlushPendingUpsertsAsync()
    {
        var keys = _pendingUpserts.Keys.ToArray();
        foreach (var key in keys)
        {
            if (!_pendingUpserts.TryRemove(key, out var pending))
            {
                continue;
            }

            pending.Cancellation.Cancel();
            await transport.EnqueueUpsertAsync(pending.Record);
        }
    }

    private void CancelPendingUpserts()
    {
        var keys = _pendingUpserts.Keys.ToArray();
        foreach (var key in keys)
        {
            if (_pendingUpserts.TryRemove(key, out var pending))
            {
                pending.Cancellation.Cancel();
            }
        }
    }

    internal Task WaitForLastApplyForTestsAsync() => _lastApplyTask;

    private async Task<bool> IsSyncEnabledAsync()
    {
        var metadata = await metadataStore.GetAsync();
        return metadata.SyncEnabled;
    }

    private void OnRecordsChanged(object? sender, IReadOnlyList<CloudSyncRecordDto> records)
    {
        _lastApplyTask = ApplyRemoteRecordsAsync(records);
    }

    private async Task ApplyRemoteRecordsAsync(IReadOnlyList<CloudSyncRecordDto> records)
    {
        foreach (var record in records)
        {
            if (record.IsTombstone)
            {
                await ApplyRemoteTombstoneAsync(record);
            }
            else
            {
                await ApplyRemoteUpsertAsync(record);
            }
        }
    }

    private async Task ApplyRemoteTombstoneAsync(CloudSyncRecordDto record)
    {
        var deletedAt = record.DeletedAt ?? record.UpdatedAt ?? DateTime.UtcNow;
        var localUpdatedAt = await GetLocalUpdatedAtAsync(record.EntityType, record.Id);

        if (localUpdatedAt is not null && !LastWriteWins.RemoteWins(localUpdatedAt, deletedAt))
        {
            return;
        }

        if (localUpdatedAt is not null)
        {
            await DeleteLocalAsync(record.EntityType, record.Id);
        }

        await tombstoneStore.UpsertAsync(new SyncTombstone
        {
            EntityType = record.EntityType,
            Id = record.Id,
            DeletedAt = deletedAt
        });
    }

    private async Task ApplyRemoteUpsertAsync(CloudSyncRecordDto record)
    {
        if (string.IsNullOrWhiteSpace(record.PayloadJson))
        {
            return;
        }

        var entity = DeserializeEntity(record);
        if (entity is null)
        {
            return;
        }

        var localUpdatedAt = await GetLocalUpdatedAtAsync(record.EntityType, record.Id);
        if (localUpdatedAt is not null && !LastWriteWins.RemoteWins(localUpdatedAt, record.UpdatedAt))
        {
            return;
        }

        ApplyRemoteMetadata(entity, record);
        await SaveLocalAsync(record.EntityType, entity);
    }

    private async Task<CloudSyncRecordDto?> BuildLocalUpsertRecordAsync(SyncEntityType type, string id, CancellationToken ct)
    {
        switch (type)
        {
            case SyncEntityType.Account:
            {
                var account = (await accountRepository.GetAccountsAsync()).FirstOrDefault(a => a.Id == id);
                if (account is null)
                {
                    return null;
                }

                if (account.UpdatedAt is null)
                {
                    account.UpdatedAt = DateTime.UtcNow;
                    await accountRepository.SaveAccountAsync(account);
                }

                return ToRecord(type, id, account, account.UpdatedAt);
            }
            case SyncEntityType.Category:
            {
                var category = (await categoryRepository.GetCategoriesAsync()).FirstOrDefault(c => c.Id == id);
                if (category is null)
                {
                    return null;
                }

                if (category.UpdatedAt is null)
                {
                    category.UpdatedAt = DateTime.UtcNow;
                    await categoryRepository.SaveCategoryAsync(category);
                }

                return ToRecord(type, id, category, category.UpdatedAt);
            }
            case SyncEntityType.Transaction:
            {
                var transaction = (await transactionRepository.GetAllTransactionsAsync(ct))
                    .FirstOrDefault(t => t.Id == id);
                if (transaction is null)
                {
                    return null;
                }

                if (transaction.UpdatedAt is null)
                {
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await transactionRepository.SaveTransactionAsync(transaction);
                }

                return ToRecord(type, id, transaction, transaction.UpdatedAt);
            }
            case SyncEntityType.RecurringTransaction:
            {
                var recurring = (await recurringTransactionRepository.GetRecurringTransactionsAsync())
                    .FirstOrDefault(r => r.Id == id);
                if (recurring is null)
                {
                    return null;
                }

                if (recurring.UpdatedAt is null)
                {
                    recurring.UpdatedAt = DateTime.UtcNow;
                    await recurringTransactionRepository.SaveRecurringTransactionAsync(recurring);
                }

                return ToRecord(type, id, recurring, recurring.UpdatedAt);
            }
            case SyncEntityType.SparZiel:
            {
                var sparZiel = (await sparZielRepository.GetSparZieleAsync()).FirstOrDefault(s => s.Id == id);
                if (sparZiel is null)
                {
                    return null;
                }

                if (sparZiel.UpdatedAt is null)
                {
                    sparZiel.UpdatedAt = DateTime.UtcNow;
                    await sparZielRepository.SaveSparZielAsync(sparZiel);
                }

                return ToRecord(type, id, sparZiel, sparZiel.UpdatedAt);
            }
            default:
                return null;
        }
    }

    private void ScheduleDebouncedUpsert((SyncEntityType Type, string Id) key, CloudSyncRecordDto record)
    {
        if (_pendingUpserts.TryGetValue(key, out var existing))
        {
            existing.Cancellation.Cancel();
        }

        var cts = new CancellationTokenSource();
        var pending = new PendingUpsert(record, cts);
        _pendingUpserts[key] = pending;

        _ = DebounceAndEnqueueAsync(key, pending);
    }

    private async Task DebounceAndEnqueueAsync((SyncEntityType Type, string Id) key, PendingUpsert pending)
    {
        try
        {
            await Task.Delay(DebounceDelay, pending.Cancellation.Token);
            if (_pendingUpserts.TryRemove(key, out var current) && ReferenceEquals(current, pending))
            {
                if (!await IsSyncEnabledAsync())
                {
                    return;
                }

                await transport.EnqueueUpsertAsync(current.Record);
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer upsert or test flush.
        }
        finally
        {
            if (_pendingUpserts.TryGetValue(key, out var current) && ReferenceEquals(current, pending))
            {
                _pendingUpserts.TryRemove(key, out _);
            }

            pending.Cancellation.Dispose();
        }
    }

    private async Task<DateTime?> GetLocalUpdatedAtAsync(SyncEntityType type, string id) =>
        type switch
        {
            SyncEntityType.Account => (await accountRepository.GetAccountsAsync())
                .FirstOrDefault(a => a.Id == id)?.UpdatedAt,
            SyncEntityType.Category => (await categoryRepository.GetCategoriesAsync())
                .FirstOrDefault(c => c.Id == id)?.UpdatedAt,
            SyncEntityType.Transaction => (await transactionRepository.GetAllTransactionsAsync())
                .FirstOrDefault(t => t.Id == id)?.UpdatedAt,
            SyncEntityType.RecurringTransaction => (await recurringTransactionRepository.GetRecurringTransactionsAsync())
                .FirstOrDefault(r => r.Id == id)?.UpdatedAt,
            SyncEntityType.SparZiel => (await sparZielRepository.GetSparZieleAsync())
                .FirstOrDefault(s => s.Id == id)?.UpdatedAt,
            _ => null
        };

    private async Task DeleteLocalAsync(SyncEntityType type, string id)
    {
        switch (type)
        {
            case SyncEntityType.Account:
                await accountRepository.DeleteAccountAsync(id);
                break;
            case SyncEntityType.Category:
                await categoryRepository.DeleteCategoryAsync(id);
                break;
            case SyncEntityType.Transaction:
                await transactionRepository.DeleteTransactionAsync(id);
                break;
            case SyncEntityType.RecurringTransaction:
                await recurringTransactionRepository.DeleteRecurringTransactionAsync(id);
                break;
            case SyncEntityType.SparZiel:
                await sparZielRepository.DeleteSparZielAsync(id);
                break;
        }
    }

    private async Task SaveLocalAsync(SyncEntityType type, object entity)
    {
        switch (type)
        {
            case SyncEntityType.Account:
                await accountRepository.SaveAccountAsync((Account)entity);
                break;
            case SyncEntityType.Category:
                await categoryRepository.SaveCategoryAsync((Category)entity);
                break;
            case SyncEntityType.Transaction:
                await transactionRepository.SaveTransactionAsync((Transaction)entity);
                break;
            case SyncEntityType.RecurringTransaction:
                await recurringTransactionRepository.SaveRecurringTransactionAsync((RecurringTransaction)entity);
                break;
            case SyncEntityType.SparZiel:
                await sparZielRepository.SaveSparZielAsync((SparZiel)entity);
                break;
        }
    }

    private static object? DeserializeEntity(CloudSyncRecordDto record)
    {
        try
        {
            return record.EntityType switch
            {
                SyncEntityType.Account => JsonSerializer.Deserialize<Account>(record.PayloadJson!, PayloadJsonOptions),
                SyncEntityType.Category => JsonSerializer.Deserialize<Category>(record.PayloadJson!, PayloadJsonOptions),
                SyncEntityType.Transaction => JsonSerializer.Deserialize<Transaction>(record.PayloadJson!, PayloadJsonOptions),
                SyncEntityType.RecurringTransaction => JsonSerializer.Deserialize<RecurringTransaction>(record.PayloadJson!, PayloadJsonOptions),
                SyncEntityType.SparZiel => JsonSerializer.Deserialize<SparZiel>(record.PayloadJson!, PayloadJsonOptions),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ApplyRemoteMetadata(object entity, CloudSyncRecordDto record)
    {
        switch (entity)
        {
            case Account account:
                account.Id = record.Id;
                account.Source = EntitySources.CloudKit;
                account.UpdatedAt = record.UpdatedAt;
                break;
            case Category category:
                category.Id = record.Id;
                category.Source = EntitySources.CloudKit;
                category.UpdatedAt = record.UpdatedAt;
                break;
            case Transaction transaction:
                transaction.Id = record.Id;
                transaction.Source = EntitySources.CloudKit;
                transaction.UpdatedAt = record.UpdatedAt;
                break;
            case RecurringTransaction recurring:
                recurring.Id = record.Id;
                recurring.Source = EntitySources.CloudKit;
                recurring.UpdatedAt = record.UpdatedAt;
                break;
            case SparZiel sparZiel:
                sparZiel.Id = record.Id;
                sparZiel.Source = EntitySources.CloudKit;
                sparZiel.UpdatedAt = record.UpdatedAt;
                break;
        }
    }

    private static CloudSyncRecordDto ToRecord<T>(SyncEntityType entityType, string id, T entity, DateTime? updatedAt) =>
        new()
        {
            EntityType = entityType,
            Id = id,
            UpdatedAt = updatedAt ?? DateTime.UtcNow,
            PayloadJson = JsonSerializer.Serialize(entity, PayloadJsonOptions),
            IsTombstone = false
        };

    private sealed class PendingUpsert(CloudSyncRecordDto record, CancellationTokenSource cancellation)
    {
        public CloudSyncRecordDto Record { get; } = record;
        public CancellationTokenSource Cancellation { get; } = cancellation;
    }
}
