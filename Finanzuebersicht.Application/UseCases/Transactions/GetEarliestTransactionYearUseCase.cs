namespace Finanzuebersicht.Application.UseCases.Transactions;

public class GetEarliestTransactionYearUseCase(ITransactionRepository transactionRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    public async Task<int?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var transactions = await _transactionRepository.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue);
        return transactions.Count > 0 ? transactions.Min(t => t.Datum.Year) : null;
    }
}
