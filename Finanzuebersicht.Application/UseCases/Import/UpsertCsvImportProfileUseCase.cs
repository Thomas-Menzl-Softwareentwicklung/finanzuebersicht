using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Application.UseCases.Import;

/// <summary>
/// Persists a user CSV import profile and notifies Cloud Sync (built-in DKB is stored as a user clone).
/// </summary>
public sealed class UpsertCsvImportProfileUseCase(
    ICsvImportProfileStore profileStore,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    public async Task ExecuteAsync(CsvImportProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (profile.IsBuiltIn)
            throw new ArgumentException("Built-in profiles must be cloned before upsert.", nameof(profile));

        CloudSyncNotify.StampUpdatedAt(profile);
        await profileStore.UpsertAsync(profile);
        await CloudSyncNotify.NotifyUpsertAsync(
            cloudSyncOrchestrator,
            SyncEntityType.CsvImportProfile,
            profile.Id,
            cancellationToken);
    }
}
