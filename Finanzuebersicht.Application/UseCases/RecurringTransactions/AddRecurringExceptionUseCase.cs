using System.Linq;
using System.Collections.Generic;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.RecurringTransactions;

public class AddRecurringExceptionUseCase(
    IRecurringTransactionRepository recurringTransactionRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(string recurringTransactionId, RecurringException exception, CancellationToken cancellationToken = default)
    {
        var list = await _recurringTransactionRepository.GetRecurringTransactionsAsync();
        var recurring = list.FirstOrDefault(r => r.Id == recurringTransactionId);
        if (recurring == null) return;

        recurring.Exceptions ??= new List<RecurringException>();
        var existing = recurring.Exceptions.FirstOrDefault(e => e.InstanceDate.Date == exception.InstanceDate.Date);
        if (existing != null)
        {
            existing.Type = exception.Type;
            existing.ShiftToDate = exception.ShiftToDate;
            existing.Note = exception.Note;
            existing.Id = exception.Id;
        }
        else
        {
            recurring.Exceptions.Add(exception);
        }

        CloudSyncNotify.StampUpdatedAt(recurring);
        await _recurringTransactionRepository.SaveRecurringTransactionAsync(recurring);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.RecurringTransaction,
            recurring.Id,
            cancellationToken);
    }
}
