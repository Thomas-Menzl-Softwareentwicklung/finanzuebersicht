using Finanzuebersicht.Core.Constants;

namespace Finanzuebersicht.Core.Services.Migrations;

/// <summary>
/// Migriert v1-Backups auf v2: ergänzt fehlende budgets.json und sparziele.json
/// mit leeren Arrays, da diese Daten-Typen in Schema-Version 1 noch nicht existierten.
/// </summary>
public class V1ToV2Migrator : IDataMigrator
{
    public int FromVersion => 1;
    public int ToVersion => 2;

    public Task<BackupArchiveData> MigrateAsync(BackupArchiveData data)
    {
        if (!data.Files.ContainsKey(DataFileNames.Budgets))
            data.Files[DataFileNames.Budgets] = "[]";

        if (!data.Files.ContainsKey(DataFileNames.Sparziele))
            data.Files[DataFileNames.Sparziele] = "[]";

        return Task.FromResult(data);
    }
}
