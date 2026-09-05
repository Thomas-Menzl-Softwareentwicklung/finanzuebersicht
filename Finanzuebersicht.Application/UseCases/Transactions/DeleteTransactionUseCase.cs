using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class DeleteTransactionUseCase(
    ITransactionRepository transactionRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        await _transactionRepository.DeleteTransactionAsync(transactionId);
        await CloudSyncNotify.NotifyDeleteAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Transaction,
            transactionId,
            cancellationToken);
    }

    public async Task ExecuteTransferGroupAsync(string transferGroupId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var transactions = await _transactionRepository.GetAllTransactionsAsync(cancellationToken);
        var ids = transactions
            .Where(t => t.TransferGroupId == transferGroupId)
            .Select(t => t.Id)
            .ToList();

        await _transactionRepository.DeleteTransferGroupAsync(transferGroupId);

        foreach (var id in ids)
        {
            await CloudSyncNotify.NotifyDeleteAsync(
                _cloudSyncOrchestrator,
                SyncEntityType.Transaction,
                id,
                cancellationToken);
        }
    }
}
