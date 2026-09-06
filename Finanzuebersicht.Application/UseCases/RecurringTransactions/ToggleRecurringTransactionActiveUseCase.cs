using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.RecurringTransactions;

public class ToggleRecurringTransactionActiveUseCase(
    IRecurringTransactionRepository recurringTransactionRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(RecurringTransaction recurringTransaction, CancellationToken cancellationToken = default)
    {
        recurringTransaction.Aktiv = !recurringTransaction.Aktiv;
        CloudSyncNotify.StampUpdatedAt(recurringTransaction);
        await _recurringTransactionRepository.SaveRecurringTransactionAsync(recurringTransaction);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.RecurringTransaction,
            recurringTransaction.Id,
            cancellationToken);
    }
}
