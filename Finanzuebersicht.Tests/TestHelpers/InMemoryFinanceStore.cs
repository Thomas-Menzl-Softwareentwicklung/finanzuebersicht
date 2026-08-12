using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.TestHelpers;

/// <summary>
/// Shared in-memory repository composite for tests. Implements all I*Repository
/// interfaces without the obsolete IDataService facade.
/// </summary>
public sealed class InMemoryFinanceStore :
    ICategoryRepository,
    IAccountRepository,
    ITransactionRepository,
    IRecurringTransactionRepository,
    IBudgetRepository,
    ISparZielRepository,
    ITransactionTemplateRepository
{
    private List<Category> _categories = [];
    private List<Account> _accounts = [];
    private List<Transaction> _transactions = [];
    private List<RecurringTransaction> _recurring = [];
    private List<CategoryBudget> _budgets = [];
    private List<SparZiel> _sparziele = [];
    private List<TransactionTemplate> _transactionTemplates = [];

    public void SetCategories(IEnumerable<Category> categories) => _categories = categories.ToList();
    public void SetAccounts(IEnumerable<Account> accounts) => _accounts = accounts.ToList();
    public void SetTransactions(IEnumerable<Transaction> transactions) => _transactions = transactions.ToList();
    public void SetRecurring(IEnumerable<RecurringTransaction> recurring) => _recurring = recurring.ToList();
    public void SetBudgets(IEnumerable<CategoryBudget> budgets) => _budgets = budgets.ToList();
    public void SetSparZiele(IEnumerable<SparZiel> sparziele) => _sparziele = sparziele.ToList();
    public void SetTransactionTemplates(IEnumerable<TransactionTemplate> templates) => _transactionTemplates = templates.ToList();

    public Task<List<Category>> GetCategoriesAsync() => Task.FromResult(_categories.ToList());
    public Task SaveCategoryAsync(Category category)
    {
        Upsert(_categories, category, c => c.Id == category.Id);
        return Task.CompletedTask;
    }
    public Task DeleteCategoryAsync(string id)
    {
        _categories.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
    public Task ReplaceAllCategoriesAsync(IEnumerable<Category> categories)
    {
        _categories = categories.ToList();
        return Task.CompletedTask;
    }

    public Task<List<Account>> GetAccountsAsync() => Task.FromResult(_accounts.ToList());
    public Task SaveAccountAsync(Account account)
    {
        Upsert(_accounts, account, a => a.Id == account.Id);
        return Task.CompletedTask;
    }
    public Task DeleteAccountAsync(string id)
    {
        _accounts.RemoveAll(a => a.Id == id);
        return Task.CompletedTask;
    }
    public Task ReplaceAllAccountsAsync(IEnumerable<Account> accounts)
    {
        _accounts = accounts.ToList();
        return Task.CompletedTask;
    }

    public Task<List<Transaction>> GetTransactionsAsync(DateTime vonDatum, DateTime bisDatum) =>
        Task.FromResult(_transactions.Where(t => t.Datum >= vonDatum && t.Datum <= bisDatum).ToList());

    public Task<List<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_transactions.ToList());

    public Task<int?> GetEarliestTransactionYearAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_transactions.Count > 0 ? (int?)_transactions.Min(t => t.Datum.Year) : null);

    public Task<bool> HasTransactionsForCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(!string.IsNullOrEmpty(categoryId) && _transactions.Any(t => t.KategorieId == categoryId));

    public Task<bool> HasTransactionsForAccountAsync(string accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(!string.IsNullOrEmpty(accountId) && _transactions.Any(t => t.AccountId == accountId));

    public Task<int> RemapCategoryIdAsync(string fromCategoryId, string toCategoryId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fromCategoryId) || string.IsNullOrEmpty(toCategoryId) || fromCategoryId == toCategoryId)
            return Task.FromResult(0);

        var changed = 0;
        foreach (var transaction in _transactions.Where(t => t.KategorieId == fromCategoryId))
        {
            transaction.KategorieId = toCategoryId;
            changed++;
        }
        return Task.FromResult(changed);
    }

    public Task<int> RemapAccountIdAsync(string fromAccountId, string toAccountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fromAccountId) || string.IsNullOrEmpty(toAccountId) || fromAccountId == toAccountId)
            return Task.FromResult(0);

        var changed = 0;
        foreach (var transaction in _transactions.Where(t => t.AccountId == fromAccountId))
        {
            transaction.AccountId = toAccountId;
            changed++;
        }
        return Task.FromResult(changed);
    }

    public Task<int> AssignMissingAccountIdsAsync(string defaultAccountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(defaultAccountId))
            return Task.FromResult(0);

        var changed = 0;
        foreach (var transaction in _transactions.Where(t => string.IsNullOrWhiteSpace(t.AccountId)))
        {
            transaction.AccountId = defaultAccountId;
            changed++;
        }
        return Task.FromResult(changed);
    }

    public Task SaveTransactionAsync(Transaction transaction)
    {
        Upsert(_transactions, transaction, t => t.Id == transaction.Id);
        return Task.CompletedTask;
    }

    public Task SaveTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
            Upsert(_transactions, transaction, t => t.Id == transaction.Id);
        return Task.CompletedTask;
    }

    public Task DeleteTransactionAsync(string id)
    {
        _transactions.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteTransferGroupAsync(string transferGroupId)
    {
        _transactions.RemoveAll(t => t.TransferGroupId == transferGroupId);
        return Task.CompletedTask;
    }

    public Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        _transactions = transactions.ToList();
        return Task.CompletedTask;
    }

    public Task<Category?> GetMostCommonCategoryForPayeeAsync(
        string payee,
        double confidenceThreshold = 0.5,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Category?>(null);

    public Task<List<RecurringTransaction>> GetRecurringTransactionsAsync() =>
        Task.FromResult(_recurring.ToList());

    public Task SaveRecurringTransactionAsync(RecurringTransaction recurring)
    {
        Upsert(_recurring, recurring, r => r.Id == recurring.Id);
        return Task.CompletedTask;
    }

    public Task DeleteRecurringTransactionAsync(string id)
    {
        _recurring.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task ReplaceAllRecurringTransactionsAsync(IEnumerable<RecurringTransaction> recurring)
    {
        _recurring = recurring.ToList();
        return Task.CompletedTask;
    }

    public Task<List<CategoryBudget>> GetBudgetsAsync() => Task.FromResult(_budgets.ToList());
    public Task SaveBudgetAsync(CategoryBudget budget)
    {
        Upsert(_budgets, budget, b => b.Id == budget.Id);
        return Task.CompletedTask;
    }
    public Task DeleteBudgetAsync(string id)
    {
        _budgets.RemoveAll(b => b.Id == id);
        return Task.CompletedTask;
    }
    public Task<CategoryBudget?> GetBudgetForCategoryAsync(string kategorieId, int year, int month) =>
        Task.FromResult<CategoryBudget?>(null);
    public Task ReplaceAllBudgetsAsync(IEnumerable<CategoryBudget> budgets)
    {
        _budgets = budgets.ToList();
        return Task.CompletedTask;
    }

    public Task<List<SparZiel>> GetSparZieleAsync() => Task.FromResult(_sparziele.ToList());
    public Task SaveSparZielAsync(SparZiel sparZiel)
    {
        Upsert(_sparziele, sparZiel, s => s.Id == sparZiel.Id);
        return Task.CompletedTask;
    }
    public Task DeleteSparZielAsync(string id)
    {
        _sparziele.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }
    public Task ReplaceAllSparZieleAsync(IEnumerable<SparZiel> sparziele)
    {
        _sparziele = sparziele.ToList();
        return Task.CompletedTask;
    }

    public Task<List<TransactionTemplate>> GetTransactionTemplatesAsync() =>
        Task.FromResult(_transactionTemplates.ToList());
    public Task SaveTransactionTemplateAsync(TransactionTemplate template)
    {
        Upsert(_transactionTemplates, template, t => t.Id == template.Id);
        return Task.CompletedTask;
    }
    public Task DeleteTransactionTemplateAsync(string id)
    {
        _transactionTemplates.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }
    public Task ReplaceAllTransactionTemplatesAsync(IEnumerable<TransactionTemplate> templates)
    {
        _transactionTemplates = templates.ToList();
        return Task.CompletedTask;
    }

    private static void Upsert<T>(List<T> items, T item, Func<T, bool> match)
    {
        var idx = items.FindIndex(x => match(x));
        if (idx >= 0) items[idx] = item;
        else items.Add(item);
    }
}

/// <summary>
/// Repository composite whose ReplaceAll methods always fail (for restore rollback tests).
/// </summary>
public sealed class FailingInMemoryFinanceStore :
    ICategoryRepository,
    IAccountRepository,
    ITransactionRepository,
    IRecurringTransactionRepository,
    IBudgetRepository,
    ISparZielRepository
{
    private static Task Fail() => Task.FromException(new InvalidOperationException("Simulated write failure"));

    public Task<List<Category>> GetCategoriesAsync() => Task.FromResult(new List<Category>());
    public Task SaveCategoryAsync(Category category) => Task.CompletedTask;
    public Task DeleteCategoryAsync(string id) => Task.CompletedTask;
    public Task ReplaceAllCategoriesAsync(IEnumerable<Category> categories) => Fail();

    public Task<List<Account>> GetAccountsAsync() => Task.FromResult(new List<Account>());
    public Task SaveAccountAsync(Account account) => Task.CompletedTask;
    public Task DeleteAccountAsync(string id) => Task.CompletedTask;
    public Task ReplaceAllAccountsAsync(IEnumerable<Account> accounts) => Fail();

    public Task<List<Transaction>> GetTransactionsAsync(DateTime vonDatum, DateTime bisDatum) =>
        Task.FromResult(new List<Transaction>());
    public Task<List<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<Transaction>());
    public Task<int?> GetEarliestTransactionYearAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(null);
    public Task<bool> HasTransactionsForCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    public Task<bool> HasTransactionsForAccountAsync(string accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    public Task<int> RemapCategoryIdAsync(string fromCategoryId, string toCategoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    public Task<int> RemapAccountIdAsync(string fromAccountId, string toAccountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    public Task<int> AssignMissingAccountIdsAsync(string defaultAccountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    public Task SaveTransactionAsync(Transaction transaction) => Task.CompletedTask;
    public Task SaveTransactionsAsync(IEnumerable<Transaction> transactions) => Task.CompletedTask;
    public Task DeleteTransactionAsync(string id) => Task.CompletedTask;
    public Task DeleteTransferGroupAsync(string transferGroupId) => Task.CompletedTask;
    public Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> transactions) => Fail();
    public Task<Category?> GetMostCommonCategoryForPayeeAsync(
        string payee,
        double confidenceThreshold = 0.5,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Category?>(null);

    public Task<List<RecurringTransaction>> GetRecurringTransactionsAsync() =>
        Task.FromResult(new List<RecurringTransaction>());
    public Task SaveRecurringTransactionAsync(RecurringTransaction recurring) => Task.CompletedTask;
    public Task DeleteRecurringTransactionAsync(string id) => Task.CompletedTask;
    public Task ReplaceAllRecurringTransactionsAsync(IEnumerable<RecurringTransaction> recurring) => Fail();

    public Task<List<CategoryBudget>> GetBudgetsAsync() => Task.FromResult(new List<CategoryBudget>());
    public Task SaveBudgetAsync(CategoryBudget budget) => Task.CompletedTask;
    public Task DeleteBudgetAsync(string id) => Task.CompletedTask;
    public Task<CategoryBudget?> GetBudgetForCategoryAsync(string kategorieId, int year, int month) =>
        Task.FromResult<CategoryBudget?>(null);
    public Task ReplaceAllBudgetsAsync(IEnumerable<CategoryBudget> budgets) => Fail();

    public Task<List<SparZiel>> GetSparZieleAsync() => Task.FromResult(new List<SparZiel>());
    public Task SaveSparZielAsync(SparZiel sparZiel) => Task.CompletedTask;
    public Task DeleteSparZielAsync(string id) => Task.CompletedTask;
    public Task ReplaceAllSparZieleAsync(IEnumerable<SparZiel> sparziele) => Fail();
}
