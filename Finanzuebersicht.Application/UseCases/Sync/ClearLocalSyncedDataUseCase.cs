using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

/// <summary>
/// Local-only wipe of synced entity types so Enable can pull from iCloud.
/// Must not enqueue CloudKit deletes — that would erase the cloud copy.
/// </summary>
public sealed class ClearLocalSyncedDataUseCase(
    IAccountRepository accountRepository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringTransactionRepository,
    ISparZielRepository sparZielRepository,
    IBudgetRepository budgetRepository,
    ITransactionTemplateRepository transactionTemplateRepository,
    ISyncTombstoneStore tombstoneStore,
    ISyncMetadataStore metadataStore)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await transactionRepository.ReplaceAllTransactionsAsync([]);
        await recurringTransactionRepository.ReplaceAllRecurringTransactionsAsync([]);
        await sparZielRepository.ReplaceAllSparZieleAsync([]);

        var accounts = await accountRepository.GetAccountsAsync();
        var keptAccounts = accounts.Where(a => a.IsSystemAccount).ToList();
        await accountRepository.ReplaceAllAccountsAsync(keptAccounts);

        var categories = await categoryRepository.GetCategoriesAsync();
        var keptCategoryIds = categories
            .Where(c => !string.IsNullOrWhiteSpace(c.SystemKey))
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);
        var keptCategories = categories.Where(c => keptCategoryIds.Contains(c.Id)).ToList();
        await categoryRepository.ReplaceAllCategoriesAsync(keptCategories);

        var budgets = await budgetRepository.GetBudgetsAsync();
        await budgetRepository.ReplaceAllBudgetsAsync(
            budgets.Where(b => keptCategoryIds.Contains(b.KategorieId)));

        var fallbackAccountId = keptAccounts.FirstOrDefault()?.Id;
        var fallbackCategoryId = keptCategories.FirstOrDefault()?.Id;
        var templates = await transactionTemplateRepository.GetTransactionTemplatesAsync();
        var keptAccountIds = keptAccounts.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var template in templates)
        {
            if (template.AccountId is not null && !keptAccountIds.Contains(template.AccountId))
                template.AccountId = fallbackAccountId;
            if (!keptCategoryIds.Contains(template.KategorieId))
                template.KategorieId = fallbackCategoryId ?? template.KategorieId;
        }
        await transactionTemplateRepository.ReplaceAllTransactionTemplatesAsync(templates);

        await tombstoneStore.ClearAsync();

        var metadata = await metadataStore.GetAsync();
        metadata.LastSyncUtc = null;
        metadata.LastError = null;
        await metadataStore.SaveAsync(metadata);

        ct.ThrowIfCancellationRequested();
    }
}
