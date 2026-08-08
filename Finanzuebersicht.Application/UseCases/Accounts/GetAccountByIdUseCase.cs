using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class GetAccountByIdUseCase(IAccountRepository accountRepository)
{
    public async Task<Account?> ExecuteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var accounts = await accountRepository.GetAccountsAsync();
        return accounts.FirstOrDefault(a => a.Id == id);
    }
}
