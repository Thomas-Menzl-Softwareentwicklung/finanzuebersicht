using System.Linq;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.RecurringTransactions;

public class RemoveRecurringExceptionUseCase(
    IRecurringTransactionRepository recurringTransactionRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(string recurringTransactionId, string exceptionId, CancellationToken cancellationToken = default)
    {
        var list = await _recurringTransactionRepository.GetRecurringTransactionsAsync();
        var recurring = list.FirstOrDefault(r => r.Id == recurringTransactionId);
        if (recurring == null || recurring.Exceptions == null) return;

        var ex = recurring.Exceptions.FirstOrDefault(e => e.Id == exceptionId);
        if (ex == null) return;

        recurring.Exceptions.Remove(ex);
        CloudSyncNotify.StampUpdatedAt(recurring);
        await _recurringTransactionRepository.SaveRecurringTransactionAsync(recurring);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.RecurringTransaction,
            recurring.Id,
            cancellationToken);
    }
}
