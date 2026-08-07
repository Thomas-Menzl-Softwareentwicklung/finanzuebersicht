using Finanzuebersicht.Models;

namespace Finanzuebersicht.Core.Services;

public class InitializationService(
    ICategoryRepository categoryRepository,
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository)
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    public async Task InitializeAsync()
    {
        await EnsureDefaultAccountAsync();

        var kategorien = await _categoryRepository.GetCategoriesAsync();
        if (kategorien.Count > 0) return;

        var standardKategorien = new List<Category>
        {
            new() { Name = "Lebensmittel", Icon = "🛒", Color = "#34C759", Typ = TransactionType.Ausgabe, SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Lebensmittel },
            new() { Name = "Transport", Icon = "🚗", Color = "#007AFF", Typ = TransactionType.Ausgabe, SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Transport },
            new() { Name = "Wohnen", Icon = "🏠", Color = "#FF9500", Typ = TransactionType.Ausgabe, SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Wohnen },
            new() { Name = "Unterhaltung", Icon = "🎬", Color = "#AF52DE", Typ = TransactionType.Ausgabe, SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Unterhaltung },
            new() { Name = "Gesundheit", Icon = "💊", Color = "#FF2D55", Typ = TransactionType.Ausgabe, SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Gesundheit },
            new() { Name = "Gehalt", Icon = "💼", Color = "#34C759", Typ = TransactionType.Einnahme, SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Gehalt },
            new() { Name = "Sonstiges", Icon = "📦", Color = "#A2845E", Typ = TransactionType.Ausgabe, SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges },
        };

        foreach (var kategorie in standardKategorien)
        {
            await _categoryRepository.SaveCategoryAsync(kategorie);
        }
    }

    private async Task EnsureDefaultAccountAsync()
    {
        var accounts = await _accountRepository.GetAccountsAsync();
        var defaultAccount = accounts.FirstOrDefault(a => a.SystemKey == Finanzuebersicht.Constants.SystemAccountKeys.Default)
            ?? accounts.FirstOrDefault();

        if (defaultAccount == null)
        {
            defaultAccount = new Account
            {
                Name = "Girokonto",
                Type = AccountType.Girokonto,
                SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default
            };
            await _accountRepository.SaveAccountAsync(defaultAccount);
        }

        await _transactionRepository.AssignMissingAccountIdsAsync(defaultAccount.Id);
    }
}
