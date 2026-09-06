using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Sync;

internal static class CloudSyncNotify
{
    internal static void StampUpdatedAt(Account entity) => entity.UpdatedAt = DateTime.UtcNow;

    internal static void StampUpdatedAt(Category entity) => entity.UpdatedAt = DateTime.UtcNow;

    internal static void StampUpdatedAt(Transaction entity) => entity.UpdatedAt = DateTime.UtcNow;

    internal static void StampUpdatedAt(RecurringTransaction entity) => entity.UpdatedAt = DateTime.UtcNow;

    internal static void StampUpdatedAt(SparZiel entity) => entity.UpdatedAt = DateTime.UtcNow;

    internal static Task NotifyUpsertAsync(
        ICloudSyncOrchestrator? orchestrator,
        SyncEntityType type,
        string id,
        CancellationToken ct = default) =>
        orchestrator is null
            ? Task.CompletedTask
            : orchestrator.NotifyLocalUpsertAsync(type, id, ct);

    internal static Task NotifyDeleteAsync(
        ICloudSyncOrchestrator? orchestrator,
        SyncEntityType type,
        string id,
        CancellationToken ct = default) =>
        orchestrator is null
            ? Task.CompletedTask
            : orchestrator.NotifyLocalDeleteAsync(type, id, ct);
}
