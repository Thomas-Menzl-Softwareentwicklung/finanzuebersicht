using System.Text.Json;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed partial class CloudSyncOrchestrator
{
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
}
