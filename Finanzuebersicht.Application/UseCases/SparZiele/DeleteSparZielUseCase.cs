using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.SparZiele;

public class DeleteSparZielUseCase(
    ISparZielRepository sparZielRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    public async Task ExecuteAsync(string id, CancellationToken cancellationToken = default)
    {
        await sparZielRepository.DeleteSparZielAsync(id);
        await CloudSyncNotify.NotifyDeleteAsync(
            cloudSyncOrchestrator,
            SyncEntityType.SparZiel,
            id,
            cancellationToken);
    }
}
