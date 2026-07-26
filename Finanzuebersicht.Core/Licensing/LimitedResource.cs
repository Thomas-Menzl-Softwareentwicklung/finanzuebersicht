namespace Finanzuebersicht.Core.Licensing;

/// <summary>Resources with Free-tier soft limits (creates only; existing data is grandfathered).</summary>
public enum LimitedResource
{
    Accounts = 1,
    RecurringTransactions = 2,
    SparZiele = 3,
    Templates = 4
}
