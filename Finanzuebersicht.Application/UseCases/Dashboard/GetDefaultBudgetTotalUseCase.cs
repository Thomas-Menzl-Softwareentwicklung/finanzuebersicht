namespace Finanzuebersicht.Application.UseCases.Dashboard;

public class GetDefaultBudgetTotalUseCase(IBudgetRepository budgetRepository)
{
    private readonly IBudgetRepository _budgetRepository = budgetRepository;

    public async Task<decimal> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var budgets = await _budgetRepository.GetBudgetsAsync() ?? [];
        return budgets
            .Where(b => b.Monat == null && b.Jahr == null && b.Betrag > 0)
            .Sum(b => b.Betrag);
    }
}
