namespace Finanzuebersicht.Core.Sync;

public enum SyncEntityType
{
    Account = 0,
    Category = 1,
    Transaction = 2,
    RecurringTransaction = 3,
    SparZiel = 4,
    SyncMeta = 5,
    /// <summary>User CSV column-mapping profile (not built-in DKB).</summary>
    CsvImportProfile = 6
}
