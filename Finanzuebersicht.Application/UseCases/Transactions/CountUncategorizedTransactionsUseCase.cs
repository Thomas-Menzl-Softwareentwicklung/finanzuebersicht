using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class CountUncategorizedTransactionsUseCase(
    ITransactionRepository transactionRepository,
    IUncategorizedCategoryService uncategorizedCategoryService)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IUncategorizedCategoryService _uncategorizedCategoryService = uncategorizedCategoryService;

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var categoryId = await _uncategorizedCategoryService.FindIdAsync(cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(categoryId))
            return 0;

        cancellationToken.ThrowIfCancellationRequested();
        var transactions = await _transactionRepository.GetTransactionsAsync(
            new DateTime(1900, 1, 1),
            new DateTime(2100, 12, 31, 23, 59, 59)).ConfigureAwait(false);

        return transactions.Count(t =>
            !t.IsTransfer &&
            string.Equals(t.KategorieId, categoryId, StringComparison.Ordinal));
    }
}
