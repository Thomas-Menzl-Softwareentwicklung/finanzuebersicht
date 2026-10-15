namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// Public support contact used for in-app mailto feature requests (#349).
/// ASC Support URL remains the website; mailto is only used inside the app.
/// </summary>
public static class SupportContact
{
    public const string Email = "info@thomasmenzl.de";

    public static string CreateLanguageRequestMailto(string subject) =>
        $"mailto:{Email}?subject={Uri.EscapeDataString(subject)}";
}
