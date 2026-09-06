using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.RecurringTransactions;

public class DeleteRecurringTransactionUseCase(
    IRecurringTransactionRepository recurringTransactionRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(string recurringTransactionId, CancellationToken cancellationToken = default)
    {
        await _recurringTransactionRepository.DeleteRecurringTransactionAsync(recurringTransactionId);
        await CloudSyncNotify.NotifyDeleteAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.RecurringTransaction,
            recurringTransactionId,
            cancellationToken);
    }
}
