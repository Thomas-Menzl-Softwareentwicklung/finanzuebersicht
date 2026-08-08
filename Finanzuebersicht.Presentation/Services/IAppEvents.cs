namespace Finanzuebersicht.Presentation.Services;

/// <summary>
/// App-wide notifications for data, language, and currency changes.
/// Presentation and MAUI pages subscribe here instead of static <c>App</c> events.
/// </summary>
public interface IAppEvents
{
    event Action? DataChanged;
    event Action? LanguageChanged;
    event Action? CurrencyChanged;

    void NotifyDataChanged();
    void NotifyLanguageChanged();
    void NotifyCurrencyChanged();
}
