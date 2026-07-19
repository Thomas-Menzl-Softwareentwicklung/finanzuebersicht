namespace Finanzuebersicht.Core.Constants;

/// <summary>
/// Canonical JSON file names for on-disk stores and backup archives.
/// Values must stay stable for serialization/backup compatibility.
/// Note: <see cref="TransactionTemplates"/> differs from
/// <see cref="BackupEntityKeys.TransactionTemplates"/> (file vs metadata entity key).
/// </summary>
public static class DataFileNames
{
    public const string Categories = "categories.json";
    public const string Accounts = "accounts.json";
    public const string Transactions = "transactions.json";
    public const string Recurring = "recurring.json";
    public const string Budgets = "budgets.json";
    public const string Sparziele = "sparziele.json";
    public const string TransactionTemplates = "transaction-templates.json";
    public const string BackupMetadata = "backup.metadata.json";
}
