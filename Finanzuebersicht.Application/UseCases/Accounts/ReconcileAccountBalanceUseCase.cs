using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class ReconcileAccountBalanceUseCase(
    IAccountRepository accountRepository,
    GetAccountBalancesUseCase getAccountBalancesUseCase,
    SaveAccountDetailUseCase saveAccountDetailUseCase)
{
    public async Task<UseCaseResult<AccountBalanceReconciliationResult>> ExecuteAsync(
        string accountId,
        decimal actualBalance,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var accounts = await accountRepository.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.Id == accountId);
        if (account is null)
            return UseCaseResult.Fail<AccountBalanceReconciliationResult>(UseCaseErrorCode.AccountNotFound);

        var summaries = await getAccountBalancesUseCase.ExecuteAsync(cancellationToken);
        var summary = summaries.FirstOrDefault(s => s.AccountId == accountId);
        if (summary is null)
            return UseCaseResult.Fail<AccountBalanceReconciliationResult>(UseCaseErrorCode.AccountBalanceNotFound);

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
            return UseCaseResult.Fail<AccountBalanceReconciliationResult>(
                saveResult.Error!.Code,
                saveResult.Error.FormatArgs.ToArray());

        return UseCaseResult.Ok(new AccountBalanceReconciliationResult
        {
            CalculatedBalance = summary.Saldo,
            ActualBalance = actualBalance,
            Delta = delta,
            NewOpeningBalance = newOpeningBalance
        });
    }
}

public class AccountBalanceReconciliationResult
{
    public decimal CalculatedBalance { get; set; }
    public decimal ActualBalance { get; set; }
    public decimal Delta { get; set; }
    public decimal NewOpeningBalance { get; set; }
}
