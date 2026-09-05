using System.Text.Json;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed class EnableCloudSyncUseCase(
    ICloudSyncTransport transport,
    ISyncMetadataStore metadataStore,
    IAccountRepository accountRepository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringTransactionRepository,
    ISparZielRepository sparZielRepository,
    ILicenseService licenseService)
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<EnableCloudSyncResult> ExecuteAsync(CancellationToken ct = default)
    {
        if (!licenseService.CanUseCloudSync)
        {
            return Blocked(EnableCloudSyncStatus.BlockedNoEntitlement, "EnableCloudSync.BlockedNoEntitlement");
        }

        if (!transport.IsSupported)
        {
            return Blocked(EnableCloudSyncStatus.BlockedUnsupported, "EnableCloudSync.BlockedUnsupported");
        }

        var accountStatus = await transport.GetAccountStatusAsync(ct);
        if (accountStatus != CloudSyncAccountStatus.Available)
        {
            return Blocked(EnableCloudSyncStatus.BlockedNoICloud, "EnableCloudSync.BlockedNoICloud");
        }

        var localEmpty = await IsLocalSyncDataEmptyAsync(ct);
        var cloudEmpty = await transport.IsZoneEmptyAsync(ct);

        if (!localEmpty && !cloudEmpty)
        {
            return Blocked(EnableCloudSyncStatus.BlockedBothHaveData, "EnableCloudSync.BlockedBothHaveData");
        }

        var metadata = await metadataStore.GetAsync();
        metadata.SyncEnabled = true;
        metadata.SchemaVersionSeen = CloudSyncSchema.CurrentVersion;
        await metadataStore.SaveAsync(metadata);
        await transport.StartAsync(ct);
        await transport.EnqueueUpsertAsync(CreateSyncMetaRecord(), ct);

        if (cloudEmpty && !localEmpty)
        {
            await SeedLocalEntitiesAsync(ct);
            await transport.SendChangesAsync(ct);
        }
        else
        {
            await transport.FetchChangesAsync(ct);
        }

        return new EnableCloudSyncResult { Status = EnableCloudSyncStatus.Enabled };
    }

    /// <summary>
    /// Local sync data is empty when there is no user-created content: zero transactions,
    /// zero non-system accounts, zero non-system categories, zero recurring transactions,
    /// and zero Sparziele. System-seeded accounts and categories alone count as empty.
    /// </summary>
    private async Task<bool> IsLocalSyncDataEmptyAsync(CancellationToken ct)
    {
        var accounts = await accountRepository.GetAccountsAsync();
        if (accounts.Any(a => !a.IsSystemAccount))
        {
            return false;
        }

        var categories = await categoryRepository.GetCategoriesAsync();
        if (categories.Any(c => string.IsNullOrWhiteSpace(c.SystemKey)))
        {
            return false;
        }

        var transactions = await transactionRepository.GetAllTransactionsAsync(ct);
        if (transactions.Count > 0)
        {
            return false;
        }

        var recurring = await recurringTransactionRepository.GetRecurringTransactionsAsync();
        if (recurring.Count > 0)
        {
            return false;
        }

        var sparZiele = await sparZielRepository.GetSparZieleAsync();
        return sparZiele.Count == 0;
    }

    private async Task SeedLocalEntitiesAsync(CancellationToken ct)
    {
        var accounts = await accountRepository.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await StampAndSaveAccountAsync(account);
            await transport.EnqueueUpsertAsync(ToRecord(SyncEntityType.Account, account.Id, account, account.UpdatedAt), ct);
        }

        var categories = await categoryRepository.GetCategoriesAsync();
        foreach (var category in categories)
        {
            await StampAndSaveCategoryAsync(category);
            await transport.EnqueueUpsertAsync(ToRecord(SyncEntityType.Category, category.Id, category, category.UpdatedAt), ct);
        }

        var transactions = await transactionRepository.GetAllTransactionsAsync(ct);
        foreach (var transaction in transactions)
        {
            await StampAndSaveTransactionAsync(transaction);
            await transport.EnqueueUpsertAsync(
                ToRecord(SyncEntityType.Transaction, transaction.Id, transaction, transaction.UpdatedAt),
                ct);
        }

        var recurring = await recurringTransactionRepository.GetRecurringTransactionsAsync();
        foreach (var item in recurring)
        {
            await StampAndSaveRecurringAsync(item);
            await transport.EnqueueUpsertAsync(
                ToRecord(SyncEntityType.RecurringTransaction, item.Id, item, item.UpdatedAt),
                ct);
        }

        var sparZiele = await sparZielRepository.GetSparZieleAsync();
        foreach (var sparZiel in sparZiele)
        {
            await StampAndSaveSparZielAsync(sparZiel);
            await transport.EnqueueUpsertAsync(
                ToRecord(SyncEntityType.SparZiel, sparZiel.Id, sparZiel, sparZiel.UpdatedAt),
                ct);
        }
    }

    private async Task StampAndSaveAccountAsync(Account account)
    {
        if (account.UpdatedAt is not null)
        {
            return;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await accountRepository.SaveAccountAsync(account);
    }

    private async Task StampAndSaveCategoryAsync(Category category)
    {
        if (category.UpdatedAt is not null)
        {
            return;
        }

        category.UpdatedAt = DateTime.UtcNow;
        await categoryRepository.SaveCategoryAsync(category);
    }

    private async Task StampAndSaveTransactionAsync(Transaction transaction)
    {
        if (transaction.UpdatedAt is not null)
        {
            return;
        }

        transaction.UpdatedAt = DateTime.UtcNow;
        await transactionRepository.SaveTransactionAsync(transaction);
    }

    private async Task StampAndSaveRecurringAsync(RecurringTransaction item)
    {
        if (item.UpdatedAt is not null)
        {
            return;
        }

        item.UpdatedAt = DateTime.UtcNow;
        await recurringTransactionRepository.SaveRecurringTransactionAsync(item);
    }

    private async Task StampAndSaveSparZielAsync(SparZiel sparZiel)
    {
        if (sparZiel.UpdatedAt is not null)
        {
            return;
        }

        sparZiel.UpdatedAt = DateTime.UtcNow;
        await sparZielRepository.SaveSparZielAsync(sparZiel);
    }

    private static CloudSyncRecordDto CreateSyncMetaRecord() =>
        new()
        {
            EntityType = SyncEntityType.SyncMeta,
            Id = CloudSyncSchema.RecordName,
            UpdatedAt = DateTime.UtcNow,
            PayloadJson = JsonSerializer.Serialize(
                new Dictionary<string, int> { ["schemaVersion"] = CloudSyncSchema.CurrentVersion },
                PayloadJsonOptions),
            IsTombstone = false
        };

    private static CloudSyncRecordDto ToRecord<T>(SyncEntityType entityType, string id, T entity, DateTime? updatedAt)
    {
        return new CloudSyncRecordDto
        {
            EntityType = entityType,
            Id = id,
            UpdatedAt = updatedAt ?? DateTime.UtcNow,
            PayloadJson = JsonSerializer.Serialize(entity, PayloadJsonOptions),
            IsTombstone = false
        };
    }

    private static EnableCloudSyncResult Blocked(EnableCloudSyncStatus status, string messageKey) =>
        new() { Status = status, MessageKey = messageKey };
}
