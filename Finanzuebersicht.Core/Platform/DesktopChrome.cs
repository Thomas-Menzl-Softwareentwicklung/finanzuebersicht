namespace Finanzuebersicht.Core.Platform;

/// <summary>
/// Mac Catalyst (desktop) chrome switches for Welle-1 UX (#350–#353).
/// Windows can later flip <see cref="IsDesktop"/> without duplicating page logic.
/// </summary>
public static class DesktopChrome
{
    /// <summary>
    /// Soft upper bound for optional future two-column / card layouts.
    /// Tab pages use full width + side padding via <c>DesktopContentHost</c> (no 720px phone column).
    /// </summary>
    public const double ContentMaxWidth = 1200;

    public static bool IsDesktop => OperatingSystem.IsMacCatalyst();

    public static bool IsPhone => !IsDesktop;
}
