using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class LoadActiveAccountsUseCase(IAccountRepository accountRepository)
{
    private readonly IAccountRepository _accountRepository = accountRepository;

    public async Task<List<Account>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var accounts = await _accountRepository.GetAccountsAsync();
        return accounts
            .Where(a => !a.IsArchived)
            .OrderBy(a => a.Name)
            .ToList();
    }
}
