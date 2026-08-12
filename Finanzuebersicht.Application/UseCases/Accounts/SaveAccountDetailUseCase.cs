using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class SaveAccountDetailUseCase(
    IAccountRepository accountRepository,
    ILicenseService? licenseService = null)
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ILicenseService _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;

    public async Task<UseCaseResult<Account>> ExecuteAsync(
        Account? existingAccount,
        string name,
        AccountType type,
        bool isArchived = false,
        decimal openingBalance = 0m,
        DateTime? openingBalanceDate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (existingAccount == null)
        {
            var accounts = await _accountRepository.GetAccountsAsync() ?? [];
            var limitCheck = _licenseService.CheckCreateLimit(LimitedResource.Accounts, accounts.Count);
            if (!limitCheck.Allowed)
            {
                return UseCaseResult.Fail<Account>(
                    UseCaseErrorCode.LicenseLimitReached,
                    limitCheck.CurrentCount,
                    limitCheck.Limit ?? 0);
            }
        }

        var account = existingAccount ?? new Account();
        account.Name = name;
        account.Type = type;
        account.IsArchived = account.IsSystemAccount ? false : isArchived;
        account.OpeningBalance = openingBalance;
        account.OpeningBalanceDate = openingBalanceDate;

        await _accountRepository.SaveAccountAsync(account);
        return UseCaseResult.Ok(account);
    }
}
