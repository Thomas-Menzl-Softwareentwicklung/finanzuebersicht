using Finanzuebersicht.Models;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// Composite data service that coordinates specialized JSON stores and exposes them
/// via repository interfaces. Reporting and recurring generation are registered
/// separately in DI (<see cref="IReportingService"/>, <see cref="IRecurringGenerationService"/>).
/// </summary>
public class LocalDataService :
    ICategoryRepository,
    IAccountRepository,
    ITransactionRepository,
    IRecurringTransactionRepository,
    IBudgetRepository,
    ISparZielRepository,
    ITransactionTemplateRepository,
    IDisposable
{
    private readonly CategoryStore _categoryStore;
    private readonly AccountStore _accountStore;
    private readonly TransactionStore _transactionStore;
    private readonly RecurringStore _recurringStore;
    private readonly BudgetStore _budgetStore;
    private readonly SparZielStore _sparZielStore;
    private readonly TransactionTemplateStore _transactionTemplateStore;

    /// <summary>
    /// Constructor for DI: receives pre-configured stores from the container.
    /// </summary>
    public LocalDataService(
        CategoryStore categoryStore,
        AccountStore accountStore,
        TransactionStore transactionStore,
        RecurringStore recurringStore,
        BudgetStore budgetStore,
        SparZielStore sparZielStore,
        TransactionTemplateStore transactionTemplateStore)
    {
        _categoryStore = categoryStore;
        _accountStore = accountStore;
        _transactionStore = transactionStore;
        _recurringStore = recurringStore;
        _budgetStore = budgetStore;
        _sparZielStore = sparZielStore;
        _transactionTemplateStore = transactionTemplateStore;
    }

    /// <summary>
    /// Alternative constructor for manual instantiation (e.g., in tests).
    /// </summary>
    public LocalDataService(ISettingsService? settings)
    {
        var dataDir = settings is null
            ? AppPaths.GetDefaultDataDir()
            : DataPathResolver.ResolveDataDir(settings);

        _categoryStore = new CategoryStore(dataDir);
        _accountStore = new AccountStore(dataDir);
        _transactionStore = new TransactionStore(dataDir, categoryStore: _categoryStore);
        _recurringStore = new RecurringStore(dataDir);
        _budgetStore = new BudgetStore(dataDir);
        _sparZielStore = new SparZielStore(dataDir);
        _transactionTemplateStore = new TransactionTemplateStore(dataDir);
    }

    #region ICategoryRepository delegation

    public async Task<List<Category>> GetCategoriesAsync()
        => await _categoryStore.GetCategoriesAsync();

    public async Task SaveCategoryAsync(Category category)
        => await _categoryStore.SaveCategoryAsync(category);

    public async Task DeleteCategoryAsync(string id)
        => await _categoryStore.DeleteCategoryAsync(id);

    public Task ReplaceAllCategoriesAsync(IEnumerable<Category> categories)
        => _categoryStore.ReplaceAllCategoriesAsync(categories);

    #endregion

    #region IAccountRepository delegation

    public Task<List<Account>> GetAccountsAsync()
        => _accountStore.GetAccountsAsync();

    public Task SaveAccountAsync(Account account)
        => _accountStore.SaveAccountAsync(account);

    public Task DeleteAccountAsync(string id)
        => _accountStore.DeleteAccountAsync(id);

    public Task ReplaceAllAccountsAsync(IEnumerable<Account> accounts)
        => _accountStore.ReplaceAllAccountsAsync(accounts);

    #endregion

    #region ITransactionRepository delegation

    public async Task<List<Transaction>> GetTransactionsAsync(DateTime vonDatum, DateTime bisDatum)
        => await _transactionStore.GetTransactionsAsync(vonDatum, bisDatum);

    public Task<List<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default)
        => _transactionStore.GetAllTransactionsAsync(cancellationToken);

    public Task<int?> GetEarliestTransactionYearAsync(CancellationToken cancellationToken = default)
        => _transactionStore.GetEarliestTransactionYearAsync(cancellationToken);

    public Task<bool> HasTransactionsForCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
        => _transactionStore.HasTransactionsForCategoryAsync(categoryId, cancellationToken);

    public Task<bool> HasTransactionsForAccountAsync(string accountId, CancellationToken cancellationToken = default)
        => _transactionStore.HasTransactionsForAccountAsync(accountId, cancellationToken);

    public Task<int> RemapCategoryIdAsync(string fromCategoryId, string toCategoryId, CancellationToken cancellationToken = default)
        => _transactionStore.RemapCategoryIdAsync(fromCategoryId, toCategoryId, cancellationToken);

    public Task<int> RemapAccountIdAsync(string fromAccountId, string toAccountId, CancellationToken cancellationToken = default)
        => _transactionStore.RemapAccountIdAsync(fromAccountId, toAccountId, cancellationToken);

    public Task<int> AssignMissingAccountIdsAsync(string defaultAccountId, CancellationToken cancellationToken = default)
        => _transactionStore.AssignMissingAccountIdsAsync(defaultAccountId, cancellationToken);

    public async Task SaveTransactionAsync(Transaction transaction)
        => await _transactionStore.SaveTransactionAsync(transaction);

    public async Task SaveTransactionsAsync(IEnumerable<Transaction> transactions)
        => await _transactionStore.SaveTransactionsAsync(transactions);

    public async Task DeleteTransactionAsync(string id)
        => await _transactionStore.DeleteTransactionAsync(id);

    public async Task DeleteTransferGroupAsync(string transferGroupId)
        => await _transactionStore.DeleteTransferGroupAsync(transferGroupId);

    public Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> transactions)
        => _transactionStore.ReplaceAllTransactionsAsync(transactions);

    public async Task<Category?> GetMostCommonCategoryForPayeeAsync(
        string payee,
        double confidenceThreshold = 0.5,
        CancellationToken cancellationToken = default)
        => await _transactionStore.GetMostCommonCategoryForPayeeAsync(payee, confidenceThreshold, cancellationToken);

    #endregion

    #region IRecurringTransactionRepository delegation

    public async Task<List<RecurringTransaction>> GetRecurringTransactionsAsync()
        => await _recurringStore.GetRecurringTransactionsAsync();

    public async Task SaveRecurringTransactionAsync(RecurringTransaction recurring)
        => await _recurringStore.SaveRecurringTransactionAsync(recurring);

    public async Task DeleteRecurringTransactionAsync(string id)
        => await _recurringStore.DeleteRecurringTransactionAsync(id);

    public Task ReplaceAllRecurringTransactionsAsync(IEnumerable<RecurringTransaction> recurring)
        => _recurringStore.ReplaceAllRecurringTransactionsAsync(recurring);

    #endregion

    #region IBudgetRepository delegation

    public Task<List<CategoryBudget>> GetBudgetsAsync() => _budgetStore.GetBudgetsAsync();
    public Task SaveBudgetAsync(CategoryBudget budget) => _budgetStore.SaveBudgetAsync(budget);
    public Task DeleteBudgetAsync(string id) => _budgetStore.DeleteBudgetAsync(id);
    public Task<CategoryBudget?> GetBudgetForCategoryAsync(string kategorieId, int year, int month)
        => _budgetStore.GetBudgetForCategoryAsync(kategorieId, year, month);
    public Task ReplaceAllBudgetsAsync(IEnumerable<CategoryBudget> budgets)
        => _budgetStore.ReplaceAllBudgetsAsync(budgets);

    #endregion

    #region ISparZielRepository delegation

    public Task<List<SparZiel>> GetSparZieleAsync() => _sparZielStore.GetSparZieleAsync();
    public Task SaveSparZielAsync(SparZiel sparZiel) => _sparZielStore.SaveSparZielAsync(sparZiel);
    public Task DeleteSparZielAsync(string id) => _sparZielStore.DeleteSparZielAsync(id);
    public Task ReplaceAllSparZieleAsync(IEnumerable<SparZiel> sparziele)
        => _sparZielStore.ReplaceAllSparZieleAsync(sparziele);

    #endregion

    #region ITransactionTemplateRepository delegation

    public Task<List<TransactionTemplate>> GetTransactionTemplatesAsync()
        => _transactionTemplateStore.GetTransactionTemplatesAsync();

    public Task SaveTransactionTemplateAsync(TransactionTemplate template)
        => _transactionTemplateStore.SaveTransactionTemplateAsync(template);

    public Task DeleteTransactionTemplateAsync(string id)
        => _transactionTemplateStore.DeleteTransactionTemplateAsync(id);

    public Task ReplaceAllTransactionTemplatesAsync(IEnumerable<TransactionTemplate> templates)
        => _transactionTemplateStore.ReplaceAllTransactionTemplatesAsync(templates);

    #endregion

    public void Dispose()
    {
        // Intentionally left empty.
        // The injected stores are managed and disposed by the DI container.
    }
}
