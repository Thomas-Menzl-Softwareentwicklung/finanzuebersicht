using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Services;

public class AccountStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly AccountStore _store;

    public AccountStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"account_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _store = new AccountStore(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public async Task SaveAndGet_ReturnsSavedAccount()
    {
        var account = new Account
        {
            Name = "Girokonto",
            Type = AccountType.Girokonto
        };

        await _store.SaveAccountAsync(account);
        var accounts = await _store.GetAccountsAsync();

        Assert.Single(accounts, a => a.Id == account.Id && a.Name == "Girokonto");
    }

    [Fact]
    public async Task SaveAndGet_PersistsOptionalSyncFields()
    {
        var updatedAt = new DateTime(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);
        var account = new Account
        {
            Name = "Girokonto",
            Type = AccountType.Girokonto,
            ExternalId = "ck-acc-1",
            Source = EntitySources.CloudKit,
            UpdatedAt = updatedAt
        };

        await _store.SaveAccountAsync(account);
        var loaded = (await _store.GetAccountsAsync()).Single(a => a.Id == account.Id);

        Assert.Equal("ck-acc-1", loaded.ExternalId);
        Assert.Equal(EntitySources.CloudKit, loaded.Source);
        Assert.Equal(updatedAt, loaded.UpdatedAt);
    }

    [Fact]
    public async Task Update_ReplacesExistingAccount()
    {
        var account = new Account { Name = "Girokonto", Type = AccountType.Girokonto };
        await _store.SaveAccountAsync(account);

        account.Name = "Hauptkonto";
        account.Type = AccountType.Tagesgeld;
        await _store.SaveAccountAsync(account);

        var accounts = await _store.GetAccountsAsync();
        Assert.Single(accounts);
        Assert.Equal("Hauptkonto", accounts[0].Name);
        Assert.Equal(AccountType.Tagesgeld, accounts[0].Type);
    }

    [Fact]
    public async Task Delete_RemovesAccount()
    {
        var account = new Account { Name = "Girokonto", Type = AccountType.Girokonto };
        await _store.SaveAccountAsync(account);

        await _store.DeleteAccountAsync(account.Id);

        var accounts = await _store.GetAccountsAsync();
        Assert.Empty(accounts);
    }

    [Fact]
    public async Task ReplaceAll_ReplacesExistingAccountsWithNewList()
    {
        await _store.SaveAccountAsync(new Account { Name = "Alt", Type = AccountType.Girokonto });

        var newAccounts = new List<Account>
        {
            new() { Name = "Giro", Type = AccountType.Girokonto },
            new() { Name = "Tagesgeld", Type = AccountType.Tagesgeld }
        };

        await _store.ReplaceAllAccountsAsync(newAccounts);

        var accounts = await _store.GetAccountsAsync();
        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, a => a.Name == "Giro");
        Assert.Contains(accounts, a => a.Name == "Tagesgeld");
    }
}
