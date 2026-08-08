namespace Finanzuebersicht.Navigation;

/// <summary>
/// Query parameter keys shared by Shell navigation senders and receivers.
/// Prefer Id keys for edit flows; entity snapshots remain for drafts (duplicate/template/contribution).
/// </summary>
public static class NavigationQueryKeys
{
    public const string Transaction = "Transaction";
    public const string TransactionId = "TransactionId";
    public const string DuplicateTransaction = "DuplicateTransaction";
    public const string TransactionTemplate = "TransactionTemplate";
    public const string SparZielContribution = "SparZielContribution";
    public const string SparZiel = "SparZiel";
    public const string RecurringTransaction = "RecurringTransaction";
    public const string RecurringId = "RecurringId";
    public const string InstanceDate = "InstanceDate";
    public const string Account = "Account";
    public const string AccountId = "AccountId";
    public const string Category = "Category";
    public const string CategoryId = "CategoryId";
    public const string Amount = "Amount";
    public const string Title = "Title";
}
