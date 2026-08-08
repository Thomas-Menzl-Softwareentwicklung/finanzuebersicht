using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class ReconcileAccountBalanceUseCase(
    IAccountRepository accountRepository,
    GetAccountBalancesUseCase getAccountBalancesUseCase,
    SaveAccountDetailUseCase saveAccountDetailUseCase)
{
    public async Task<AccountBalanceReconciliationResult> ExecuteAsync(
        string accountId,
        decimal actualBalance,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var accounts = await accountRepository.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.Id == accountId)
            ?? throw new InvalidOperationException("Account not found.");

        var summaries = await getAccountBalancesUseCase.ExecuteAsync(cancellationToken);
        var summary = summaries.FirstOrDefault(s => s.AccountId == accountId)
            ?? throw new InvalidOperationException("Account balance not found.");

        var delta = actualBalance - summary.Saldo;
        var newOpeningBalance = account.OpeningBalance + delta;

        var saveResult = await saveAccountDetailUseCase.ExecuteAsync(
            account,
            account.Name,
            account.Type,
            account.IsArchived,
            newOpeningBalance,
            account.OpeningBalanceDate,
            cancellationToken);
        if (!saveResult.IsSuccess)
            throw new InvalidOperationException($"Account save failed: {saveResult.Error!.Code}");

        return new AccountBalanceReconciliationResult
        {
            CalculatedBalance = summary.Saldo,
            ActualBalance = actualBalance,
            Delta = delta,
            NewOpeningBalance = newOpeningBalance
        };
    }
}

public class AccountBalanceReconciliationResult
{
    public decimal CalculatedBalance { get; set; }
    public decimal ActualBalance { get; set; }
    public decimal Delta { get; set; }
    public decimal NewOpeningBalance { get; set; }
}
