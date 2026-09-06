using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class DeleteAccountUseCase(
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository,
    ITransactionTemplateRepository transactionTemplateRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ITransactionTemplateRepository _transactionTemplateRepository = transactionTemplateRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(string accountId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var accounts = await _accountRepository.GetAccountsAsync();
        var target = accounts.FirstOrDefault(a => a.Id == accountId);
        if (target == null) return;

        if (target.SystemKey == SystemAccountKeys.Default)
            throw new InvalidOperationException("Default account cannot be deleted.");

        var fallback = accounts.FirstOrDefault(a => a.SystemKey == SystemAccountKeys.Default && a.Id != accountId)
            ?? accounts.FirstOrDefault(a => a.Id != accountId);

        var createdFallback = false;
        if (fallback == null)
        {
            fallback = new Account
            {
                Name = "Girokonto",
                Type = AccountType.Girokonto,
                SystemKey = SystemAccountKeys.Default
            };
            CloudSyncNotify.StampUpdatedAt(fallback);
            await _accountRepository.SaveAccountAsync(fallback);
            createdFallback = true;
        }

        var transactions = await _transactionRepository.GetAllTransactionsAsync(cancellationToken);
        var remappedTransactionIds = transactions
            .Where(t => t.AccountId == accountId)
            .Select(t => t.Id)
            .ToList();

        await _transactionRepository.RemapAccountIdAsync(accountId, fallback.Id, cancellationToken);

        foreach (var transaction in transactions.Where(t => remappedTransactionIds.Contains(t.Id)))
        {
            transaction.AccountId = fallback.Id;
            CloudSyncNotify.StampUpdatedAt(transaction);
            await _transactionRepository.SaveTransactionAsync(transaction);
            await CloudSyncNotify.NotifyUpsertAsync(
                _cloudSyncOrchestrator,
                SyncEntityType.Transaction,
                transaction.Id,
                cancellationToken);
        }

        var templates = await _transactionTemplateRepository.GetTransactionTemplatesAsync();
        foreach (var template in templates.Where(t => t.AccountId == accountId))
        {
            template.AccountId = fallback.Id;
            await _transactionTemplateRepository.SaveTransactionTemplateAsync(template);
        }

        await _accountRepository.DeleteAccountAsync(accountId);
        await CloudSyncNotify.NotifyDeleteAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Account,
            accountId,
            cancellationToken);

        if (createdFallback)
        {
            await CloudSyncNotify.NotifyUpsertAsync(
                _cloudSyncOrchestrator,
                SyncEntityType.Account,
                fallback.Id,
                cancellationToken);
        }
    }
}
