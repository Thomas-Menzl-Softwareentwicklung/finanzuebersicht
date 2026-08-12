namespace Finanzuebersicht.Core.Licensing;

/// <summary>Free-tier soft limits for Store distribution (see docs/MONETIZATION.md).</summary>
public static class FreeTierLimits
{
    public const int MaxAccounts = 3;
    public const int MaxRecurringTransactions = 8;
    public const int MaxSparZiele = 1;
    public const int MaxTemplates = 3;

    public static int GetMax(LimitedResource resource) => resource switch
    {
        LimitedResource.Accounts => MaxAccounts,
        LimitedResource.RecurringTransactions => MaxRecurringTransactions,
        LimitedResource.SparZiele => MaxSparZiele,
        LimitedResource.Templates => MaxTemplates,
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null)
    };
}
