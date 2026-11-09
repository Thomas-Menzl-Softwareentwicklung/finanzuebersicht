namespace Finanzuebersicht.Core.AppLock;

public sealed record AppLockOptions(bool Enabled, int DelaySeconds)
{
    public static AppLockOptions Default { get; } = new(false, 300);
    public static int[] AllowedDelaysSeconds { get; } = [0, 60, 300, 900];

    public void Validate()
    {
        if (!AllowedDelaysSeconds.Contains(DelaySeconds))
            throw new ArgumentOutOfRangeException(nameof(DelaySeconds), DelaySeconds, "Unsupported app lock delay.");
    }
}
