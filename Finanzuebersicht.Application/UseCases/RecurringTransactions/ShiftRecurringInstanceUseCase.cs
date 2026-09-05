using System.Linq;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.RecurringTransactions;

public class ShiftRecurringInstanceUseCase(
    IRecurringTransactionRepository recurringTransactionRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(string recurringTransactionId, DateTime instanceDate, DateTime newDate, string? note = null, CancellationToken cancellationToken = default)
    {
        var list = await _recurringTransactionRepository.GetRecurringTransactionsAsync();
        var recurring = list.FirstOrDefault(r => r.Id == recurringTransactionId);
        if (recurring == null) return;

        recurring.Exceptions ??= new List<RecurringException>();
        var existing = recurring.Exceptions.FirstOrDefault(e => e.InstanceDate.Date == instanceDate.Date);
        if (existing != null)
        {
            existing.Type = RecurringExceptionType.Shift;
            existing.ShiftToDate = newDate.Date;
            existing.Note = note;
        }
        else
        {
            var ex = new RecurringException
            {
                InstanceDate = instanceDate.Date,
                Type = RecurringExceptionType.Shift,
                ShiftToDate = newDate.Date,
                Note = note
            };
            recurring.Exceptions.Add(ex);
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
