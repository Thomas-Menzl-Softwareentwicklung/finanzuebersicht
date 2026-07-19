using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

public class BudgetStore : JsonDataStoreBase, IBudgetRepository
{
    private string BudgetsFile => Path.Combine(DataDir, DataFileNames.Budgets);

    public BudgetStore(string dataDir, ILogger<BudgetStore>? logger = null)
        : base(dataDir, logger) { }

    public async Task<List<CategoryBudget>> GetBudgetsAsync()
    {
        await StoreLock.WaitAsync();
        try { return await LoadAsync<CategoryBudget>(BudgetsFile); }
        finally { StoreLock.Release(); }
    }

    public async Task SaveBudgetAsync(CategoryBudget budget)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<CategoryBudget>(BudgetsFile);
            var idx = items.FindIndex(b => b.Id == budget.Id);
            if (idx >= 0) items[idx] = budget; else items.Add(budget);
            await SaveAsync(BudgetsFile, items);
        }
        finally { StoreLock.Release(); }
    }

    public async Task DeleteBudgetAsync(string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<CategoryBudget>(BudgetsFile);
            items.RemoveAll(b => b.Id == id);
            await SaveAsync(BudgetsFile, items);
        }
        finally { StoreLock.Release(); }
    }

    public Task ReplaceAllBudgetsAsync(IEnumerable<CategoryBudget> budgets)
        => ReplaceAllAsync(BudgetsFile, budgets);

    public async Task<CategoryBudget?> GetBudgetForCategoryAsync(string kategorieId, int year, int month)
    {
        var budgets = await GetBudgetsAsync();
        // Prefer specific month/year match, then month-only, then default (null/null)
        return budgets.FirstOrDefault(b => b.KategorieId == kategorieId && b.Jahr == year && b.Monat == month)
            ?? budgets.FirstOrDefault(b => b.KategorieId == kategorieId && b.Jahr == null && b.Monat == month)
            ?? budgets.FirstOrDefault(b => b.KategorieId == kategorieId && b.Jahr == null && b.Monat == null);
    }
}
