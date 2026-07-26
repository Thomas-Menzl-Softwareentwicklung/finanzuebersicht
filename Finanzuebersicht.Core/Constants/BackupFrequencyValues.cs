namespace Finanzuebersicht.Core.Constants;

/// <summary>
/// Persisted auto-backup frequency values (SettingsKeys.BackupFrequency).
/// </summary>
public static class BackupFrequencyValues
{
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";

    public static readonly string[] All = [Daily, Weekly, Monthly];
}
