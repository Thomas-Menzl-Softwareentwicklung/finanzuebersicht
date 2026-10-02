using System.Text.Json;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed partial class CloudSyncOrchestrator
{
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
