using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.SparZiele;

public class SaveSparZielUseCase(
    ISparZielRepository sparZielRepository,
    ILicenseService? licenseService = null,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly ILicenseService _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task ExecuteAsync(SparZiel sparZiel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await sparZielRepository.GetSparZieleAsync() ?? [];
        var isNew = existing.All(s => s.Id != sparZiel.Id);
        if (isNew)
            _licenseService.EnsureCanCreate(LimitedResource.SparZiele, existing.Count);

        CloudSyncNotify.StampUpdatedAt(sparZiel);
        await sparZielRepository.SaveSparZielAsync(sparZiel);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.SparZiel,
            sparZiel.Id,
            cancellationToken);
    }
}
