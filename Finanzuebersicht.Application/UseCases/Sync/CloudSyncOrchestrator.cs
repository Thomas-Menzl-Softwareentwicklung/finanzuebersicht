using System.Collections.Concurrent;
using System.Text.Json;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Licensing;
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

    private async Task EnqueueDirtyEntitiesAsync(SyncMetadata metadata, CancellationToken ct)
    {
        if (metadata.LastSyncUtc is null)
        {
            return;
        }

        var lastSync = metadata.LastSyncUtc.Value;
        await EnqueueDirtyOfTypeAsync(SyncEntityType.Account, lastSync, ct);
        await EnqueueDirtyOfTypeAsync(SyncEntityType.Category, lastSync, ct);
        await EnqueueDirtyOfTypeAsync(SyncEntityType.Transaction, lastSync, ct);
        await EnqueueDirtyOfTypeAsync(SyncEntityType.RecurringTransaction, lastSync, ct);
        await EnqueueDirtyOfTypeAsync(SyncEntityType.SparZiel, lastSync, ct);
    }

    private async Task EnqueueDirtyOfTypeAsync(SyncEntityType type, DateTime lastSyncUtc, CancellationToken ct)
    {
        IEnumerable<string> ids = type switch
        {
            SyncEntityType.Account => (await accountRepository.GetAccountsAsync())
                .Where(a => a.UpdatedAt > lastSyncUtc)
                .Select(a => a.Id),
            SyncEntityType.Category => (await categoryRepository.GetCategoriesAsync())
                .Where(c => c.UpdatedAt > lastSyncUtc)
                .Select(c => c.Id),
            SyncEntityType.Transaction => (await transactionRepository.GetAllTransactionsAsync(ct))
                .Where(t => t.UpdatedAt > lastSyncUtc)
                .Select(t => t.Id),
            SyncEntityType.RecurringTransaction => (await recurringTransactionRepository.GetRecurringTransactionsAsync())
                .Where(r => r.UpdatedAt > lastSyncUtc)
                .Select(r => r.Id),
            SyncEntityType.SparZiel => (await sparZielRepository.GetSparZieleAsync())
                .Where(s => s.UpdatedAt > lastSyncUtc)
                .Select(s => s.Id),
            _ => []
        };

        foreach (var id in ids)
        {
            var record = await BuildLocalUpsertRecordAsync(type, id, ct);
            if (record is not null)
            {
                await transport.EnqueueUpsertAsync(record, ct);
            }
        }
    }

    private void OnRecordsChanged(object? sender, IReadOnlyList<CloudSyncRecordDto> records)
    {
        _lastApplyTask = ApplyRemoteRecordsAsync(records);
    }

    private async Task ApplyRemoteRecordsAsync(IReadOnlyList<CloudSyncRecordDto> records)
    {
        try
        {
            if (await TryPauseForNewerSchemaAsync(records))
            {
                return;
            }

            foreach (var record in records)
            {
                if (record.EntityType == SyncEntityType.SyncMeta)
                {
                    await ApplySyncMetaAsync(record);
                    continue;
                }

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
        catch (Exception ex)
        {
            await PersistLastErrorAsync(ex.Message);
        }
    }

    private async Task<bool> TryPauseForNewerSchemaAsync(IReadOnlyList<CloudSyncRecordDto> records)
    {
        foreach (var record in records)
        {
            if (record.EntityType != SyncEntityType.SyncMeta)
            {
                continue;
            }

            var version = ParseSchemaVersion(record);
            if (version is int remoteVersion && remoteVersion > CloudSyncSchema.CurrentVersion)
            {
                var metadata = await metadataStore.GetAsync();
                metadata.SchemaVersionSeen = remoteVersion;
                metadata.LastError = CloudSyncSchema.TooNewError;
                await metadataStore.SaveAsync(metadata);
                return true;
            }
        }

        return false;
    }

    private async Task ApplySyncMetaAsync(CloudSyncRecordDto record)
    {
        var version = ParseSchemaVersion(record) ?? CloudSyncSchema.CurrentVersion;
        var metadata = await metadataStore.GetAsync();
        metadata.SchemaVersionSeen = Math.Max(metadata.SchemaVersionSeen, version);
        await metadataStore.SaveAsync(metadata);
    }

    private static int? ParseSchemaVersion(CloudSyncRecordDto record)
    {
        if (string.IsNullOrWhiteSpace(record.PayloadJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(record.PayloadJson);
            if (doc.RootElement.TryGetProperty("schemaVersion", out var property) && property.TryGetInt32(out var version))
            {
                return version;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private async Task ApplyRemoteTombstoneAsync(CloudSyncRecordDto record)
    {
        var deletedAt = record.DeletedAt ?? record.UpdatedAt ?? DateTime.UtcNow;
        var (exists, localUpdatedAt) = await GetLocalPresenceAsync(record.EntityType, record.Id);

        if (exists && !LastWriteWins.RemoteWins(localUpdatedAt, deletedAt))
        {
            return;
        }

        if (exists)
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
        await ReplaceLocalSystemDuplicateAsync(entity);
        await SaveLocalAsync(record.EntityType, entity);
    }

    private async Task ReplaceLocalSystemDuplicateAsync(object entity)
    {
        switch (entity)
        {
            case Account account when !string.IsNullOrWhiteSpace(account.SystemKey):
            {
                var duplicate = (await accountRepository.GetAccountsAsync())
                    .FirstOrDefault(a => a.SystemKey == account.SystemKey && a.Id != account.Id);
                if (duplicate is null)
                {
                    return;
                }

                await RemapAccountReferencesAsync(duplicate.Id, account.Id);
                await accountRepository.DeleteAccountAsync(duplicate.Id);
                break;
            }
            case Category category when !string.IsNullOrWhiteSpace(category.SystemKey):
            {
                var duplicate = (await categoryRepository.GetCategoriesAsync())
                    .FirstOrDefault(c => c.SystemKey == category.SystemKey && c.Id != category.Id);
                if (duplicate is null)
                {
                    return;
                }

                await RemapCategoryReferencesAsync(duplicate.Id, category.Id);
                await categoryRepository.DeleteCategoryAsync(duplicate.Id);
                break;
            }
        }
    }

    private async Task RemapAccountReferencesAsync(string fromId, string toId)
    {
        var transactions = await transactionRepository.GetAllTransactionsAsync();
        var affected = transactions.Where(t => t.AccountId == fromId).ToList();
        if (affected.Count > 0)
        {
            await transactionRepository.RemapAccountIdAsync(fromId, toId);
        }

        foreach (var transaction in affected)
        {
            transaction.AccountId = toId;
            transaction.UpdatedAt = DateTime.UtcNow;
            await transactionRepository.SaveTransactionAsync(transaction);
            await NotifyLocalUpsertAsync(SyncEntityType.Transaction, transaction.Id);
        }

        var recurring = await recurringTransactionRepository.GetRecurringTransactionsAsync();
        foreach (var item in recurring.Where(r => r.AccountId == fromId))
        {
            item.AccountId = toId;
            item.UpdatedAt = DateTime.UtcNow;
            await recurringTransactionRepository.SaveRecurringTransactionAsync(item);
            await NotifyLocalUpsertAsync(SyncEntityType.RecurringTransaction, item.Id);
        }
    }

    private async Task RemapCategoryReferencesAsync(string fromId, string toId)
    {
        var transactions = await transactionRepository.GetAllTransactionsAsync();
        var affected = transactions.Where(t => t.KategorieId == fromId).ToList();
        if (affected.Count > 0)
        {
            await transactionRepository.RemapCategoryIdAsync(fromId, toId);
        }

        foreach (var transaction in affected)
        {
            transaction.KategorieId = toId;
            transaction.UpdatedAt = DateTime.UtcNow;
            await transactionRepository.SaveTransactionAsync(transaction);
            await NotifyLocalUpsertAsync(SyncEntityType.Transaction, transaction.Id);
        }

        var recurring = await recurringTransactionRepository.GetRecurringTransactionsAsync();
        foreach (var item in recurring.Where(r => r.KategorieId == fromId))
        {
            item.KategorieId = toId;
            item.UpdatedAt = DateTime.UtcNow;
            await recurringTransactionRepository.SaveRecurringTransactionAsync(item);
            await NotifyLocalUpsertAsync(SyncEntityType.RecurringTransaction, item.Id);
        }
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

    /// <summary>
    /// Removes <paramref name="pending"/> only if it is still the dictionary value.
    /// Unconditional <c>TryRemove(key)</c> can drop a newer upsert that replaced it
    /// (empty enqueue in <c>FlushPendingForTestsAsync</c> under CI timing).
    /// </summary>
    private bool TryRemovePendingIfSame((SyncEntityType Type, string Id) key, PendingUpsert pending) =>
        _pendingUpserts.TryRemove(new KeyValuePair<(SyncEntityType Type, string Id), PendingUpsert>(key, pending));

    private async Task DebounceAndEnqueueAsync((SyncEntityType Type, string Id) key, PendingUpsert pending)
    {
        try
        {
            await Task.Delay(DebounceDelay, pending.Cancellation.Token);
            if (TryRemovePendingIfSame(key, pending))
            {
                if (!await IsSyncEnabledAsync())
                {
                    return;
                }

                await transport.EnqueueUpsertAsync(pending.Record);
                await TrySendChangesAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer upsert or test flush.
        }
        finally
        {
            TryRemovePendingIfSame(key, pending);
            pending.Cancellation.Dispose();
        }
    }

    private async Task<DateTime?> GetLocalUpdatedAtAsync(SyncEntityType type, string id)
    {
        var (_, updatedAt) = await GetLocalPresenceAsync(type, id);
        return updatedAt;
    }

    private async Task<(bool Exists, DateTime? UpdatedAt)> GetLocalPresenceAsync(SyncEntityType type, string id)
    {
        switch (type)
        {
            case SyncEntityType.Account:
            {
                var entity = (await accountRepository.GetAccountsAsync()).FirstOrDefault(a => a.Id == id);
                return (entity is not null, entity?.UpdatedAt);
            }
            case SyncEntityType.Category:
            {
                var entity = (await categoryRepository.GetCategoriesAsync()).FirstOrDefault(c => c.Id == id);
                return (entity is not null, entity?.UpdatedAt);
            }
            case SyncEntityType.Transaction:
            {
                var entity = (await transactionRepository.GetAllTransactionsAsync()).FirstOrDefault(t => t.Id == id);
                return (entity is not null, entity?.UpdatedAt);
            }
            case SyncEntityType.RecurringTransaction:
            {
                var entity = (await recurringTransactionRepository.GetRecurringTransactionsAsync())
                    .FirstOrDefault(r => r.Id == id);
                return (entity is not null, entity?.UpdatedAt);
            }
            case SyncEntityType.SparZiel:
            {
                var entity = (await sparZielRepository.GetSparZieleAsync()).FirstOrDefault(s => s.Id == id);
                return (entity is not null, entity?.UpdatedAt);
            }
            default:
                return (false, null);
        }
    }

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
