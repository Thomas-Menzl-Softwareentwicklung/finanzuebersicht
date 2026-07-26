using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

public class AccountStore : JsonDataStoreBase, IAccountRepository
{
    private string AccountsFile => Path.Combine(DataDir, DataFileNames.Accounts);

    public AccountStore(string dataDir, ILogger<AccountStore>? logger = null)
        : base(dataDir, logger)
    {
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        await StoreLock.WaitAsync();
        try
        {
            return await LoadAsync<Account>(AccountsFile);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task SaveAccountAsync(Account account)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Account>(AccountsFile);
            var idx = items.FindIndex(a => a.Id == account.Id);
            if (idx >= 0)
                items[idx] = account;
            else
                items.Add(account);
            await SaveAsync(AccountsFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task DeleteAccountAsync(string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Account>(AccountsFile);
            items.RemoveAll(a => a.Id == id);
            await SaveAsync(AccountsFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public Task ReplaceAllAccountsAsync(IEnumerable<Account> accounts)
        => ReplaceAllAsync(AccountsFile, accounts);
}
