namespace Finanzuebersicht.Core.Platform;

/// <summary>
/// Mac Catalyst (desktop) chrome switches for Welle-1 UX (#350–#353).
/// Windows can later flip <see cref="IsDesktop"/> without duplicating page logic.
/// </summary>
public static class DesktopChrome
{
    /// <summary>Readable content width on wide Mac windows (~15″+).</summary>
    public const double ContentMaxWidth = 720;

    public static bool IsDesktop => OperatingSystem.IsMacCatalyst();

    public static bool IsPhone => !IsDesktop;
}
