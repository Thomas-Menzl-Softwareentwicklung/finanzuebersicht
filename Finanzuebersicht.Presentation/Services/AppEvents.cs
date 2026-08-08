namespace Finanzuebersicht.Presentation.Services;

/// <summary>
/// Default in-process app-wide event bus.
/// </summary>
public sealed class AppEvents : IAppEvents
{
    public event Action? DataChanged;
    public event Action? LanguageChanged;
    public event Action? CurrencyChanged;

    public void NotifyDataChanged() => DataChanged?.Invoke();

    public void NotifyLanguageChanged() => LanguageChanged?.Invoke();

    public void NotifyCurrencyChanged() => CurrencyChanged?.Invoke();
}
