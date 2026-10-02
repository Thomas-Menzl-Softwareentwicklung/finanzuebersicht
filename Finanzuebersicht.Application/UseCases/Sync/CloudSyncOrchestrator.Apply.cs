using System.Text.Json;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed partial class CloudSyncOrchestrator
{
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
}
