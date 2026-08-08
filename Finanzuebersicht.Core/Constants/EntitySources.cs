namespace Finanzuebersicht.Constants;

/// <summary>
/// Known values for <c>Source</c> on syncable entities (#300).
/// Stored as plain strings so JSON stays forward-compatible.
/// </summary>
public static class EntitySources
{
    public const string CloudKit = "CloudKit";
    public const string OpenBanking = "OpenBanking";
}
