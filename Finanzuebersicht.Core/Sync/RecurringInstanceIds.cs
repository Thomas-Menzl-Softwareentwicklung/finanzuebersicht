namespace Finanzuebersicht.Core.Sync;

/// <summary>
/// Deterministic transaction ids for auto-generated recurring instances (cross-device dedup).
/// </summary>
public static class RecurringInstanceIds
{
    public static string For(string recurringId, DateTime instanceDate)
        => $"rec:{recurringId}:{instanceDate.Date:yyyy-MM-dd}";
}
