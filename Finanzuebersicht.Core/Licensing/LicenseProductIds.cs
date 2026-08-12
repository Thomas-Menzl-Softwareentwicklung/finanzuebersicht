namespace Finanzuebersicht.Core.Licensing;

/// <summary>App Store Connect product identifiers (must match ASC exactly).</summary>
public static class LicenseProductIds
{
    public const string Pro = "de.thomasmenzl.finanzuebersicht.pro";
    public const string SyncYearly = "de.thomasmenzl.finanzuebersicht.sync.yearly";

    public static IReadOnlyList<string> All { get; } = [Pro, SyncYearly];
}
