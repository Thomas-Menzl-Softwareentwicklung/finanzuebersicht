using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;

namespace Finanzuebersicht.Application.UseCases.ScreenshotDemo;

public class SeedScreenshotDemoDataUseCase(
    ICategoryRepository categoryRepository,
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringRepository,
    IBudgetRepository budgetRepository,
    ISparZielRepository sparZielRepository,
    IClock? clock = null)
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = ScreenshotDemoFixture.Create(_clock);

        await categoryRepository.ReplaceAllCategoriesAsync(snapshot.Categories);
        await accountRepository.ReplaceAllAccountsAsync(snapshot.Accounts);
        await transactionRepository.ReplaceAllTransactionsAsync(snapshot.Transactions);
        await recurringRepository.ReplaceAllRecurringTransactionsAsync(snapshot.RecurringTransactions);
        await budgetRepository.ReplaceAllBudgetsAsync(snapshot.Budgets);
        await sparZielRepository.ReplaceAllSparZieleAsync(snapshot.SparZiele);
    }
}
