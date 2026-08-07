namespace Finanzuebersicht.Application.UseCases.Transactions;

public class GetEarliestTransactionYearUseCase(ITransactionRepository transactionRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    public async Task<int?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _transactionRepository.GetEarliestTransactionYearAsync(cancellationToken);
    }
}
