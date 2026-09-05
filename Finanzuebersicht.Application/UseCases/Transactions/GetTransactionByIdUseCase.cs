using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class GetTransactionByIdUseCase(ITransactionRepository transactionRepository)
{
    public async Task<Transaction?> ExecuteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var all = await transactionRepository.GetAllTransactionsAsync(cancellationToken);
        return all.FirstOrDefault(t => t.Id == id);
    }
}
