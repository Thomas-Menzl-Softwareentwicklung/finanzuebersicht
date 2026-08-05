namespace Finanzuebersicht.Core.Constants;

/// <summary>Shared container id for the main app and WidgetKit extension.</summary>
public static class AppGroupIds
{
    public const string Finanzuebersicht = "group.de.thomasmenzl.finanzuebersicht";
    public const string QuickExpensePendingFileName = "quick-expense-pending.json";
    public const string HasProFlagKey = "hasPro";
    /// <summary>BCP-47 language tag written by the app (e.g. "de", "en"); empty = follow system.</summary>
    public const string PreferredLanguageKey = "preferredLanguage";
}
