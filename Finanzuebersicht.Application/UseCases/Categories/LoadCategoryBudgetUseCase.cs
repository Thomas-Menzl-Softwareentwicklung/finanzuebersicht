namespace Finanzuebersicht.Application.UseCases.Categories;

/// <summary>
/// Loads the default (month/year unset) budget amount for a category.
/// Returns 0 when no default budget exists.
/// </summary>
public class LoadCategoryBudgetUseCase(IBudgetRepository budgetRepository)
{
    public async Task<decimal> ExecuteAsync(string kategorieId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(kategorieId))
            return 0;

        var budgets = await budgetRepository.GetBudgetsAsync();
        var budget = budgets.FirstOrDefault(b =>
            b.KategorieId == kategorieId && b.Monat == null && b.Jahr == null);
        return budget?.Betrag ?? 0;
    }
}
