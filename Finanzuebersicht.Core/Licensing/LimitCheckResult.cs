namespace Finanzuebersicht.Core.Licensing;

public readonly record struct LimitCheckResult(bool Allowed, int CurrentCount, int? Limit)
{
    public static LimitCheckResult Unlimited(int currentCount) => new(true, currentCount, null);
}
