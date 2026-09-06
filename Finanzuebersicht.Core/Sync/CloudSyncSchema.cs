namespace Finanzuebersicht.Core.Sync;

public static class CloudSyncSchema
{
    public const int CurrentVersion = 1;
    public const string RecordName = "sync-meta";
    public const string TooNewError = "Cloud schema is newer than this app. Please update Finanzübersicht.";
}
