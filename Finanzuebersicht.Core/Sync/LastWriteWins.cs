namespace Finanzuebersicht.Core.Sync;

public static class LastWriteWins
{
    public static bool RemoteWins(DateTime? localUpdatedAt, DateTime? remoteUpdatedAt)
    {
        if (remoteUpdatedAt is null)
            return localUpdatedAt is null;
        if (localUpdatedAt is null)
            return true;
        return remoteUpdatedAt.Value >= localUpdatedAt.Value;
    }
}
