using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed class CloudSyncLocalChangeNotifier(ICloudSyncOrchestrator orchestrator) : ILocalChangeNotifier
{
    public Task NotifyTransactionUpsertAsync(string transactionId, CancellationToken cancellationToken = default)
        => CloudSyncNotify.NotifyUpsertAsync(orchestrator, SyncEntityType.Transaction, transactionId, cancellationToken);

    public Task NotifyRecurringUpsertAsync(string recurringId, CancellationToken cancellationToken = default)
        => CloudSyncNotify.NotifyUpsertAsync(orchestrator, SyncEntityType.RecurringTransaction, recurringId, cancellationToken);
}
