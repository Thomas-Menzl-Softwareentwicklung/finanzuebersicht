namespace Finanzuebersicht.Core.Sync;

/// <summary>
/// Optional hook for Core services to notify the app layer after local data changes (e.g. CloudKit sync enqueue).
/// </summary>
public interface ILocalChangeNotifier
{
    Task NotifyTransactionUpsertAsync(string transactionId, CancellationToken cancellationToken = default);

    Task NotifyRecurringUpsertAsync(string recurringId, CancellationToken cancellationToken = default);
}
