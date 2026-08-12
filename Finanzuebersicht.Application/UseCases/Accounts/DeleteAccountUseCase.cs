using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class DeleteAccountUseCase(
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository,
    ITransactionTemplateRepository transactionTemplateRepository)
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ITransactionTemplateRepository _transactionTemplateRepository = transactionTemplateRepository;

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

        if (fallback == null)
        {
            fallback = new Account
            {
                Name = "Girokonto",
                Type = AccountType.Girokonto,
                SystemKey = SystemAccountKeys.Default
            };
            await _accountRepository.SaveAccountAsync(fallback);
        }

        await _transactionRepository.RemapAccountIdAsync(accountId, fallback.Id, cancellationToken);

        var templates = await _transactionTemplateRepository.GetTransactionTemplatesAsync();
        foreach (var template in templates.Where(t => t.AccountId == accountId))
        {
            template.AccountId = fallback.Id;
            await _transactionTemplateRepository.SaveTransactionTemplateAsync(template);
        }

        await _accountRepository.DeleteAccountAsync(accountId);
    }
}
