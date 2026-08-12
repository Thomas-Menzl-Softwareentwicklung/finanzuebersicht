using System.Globalization;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.Services;

/// <summary>
/// Verwaltet die App-Sprache. Liest gespeicherte Sprachpräferenz aus SettingsService,
/// fällt auf Gerätesprache zurück und aktualisiert LocalizationResourceManager live.
/// </summary>
public class LocalizationService(ISettingsService settings) : ILocalizationService
{
    private readonly ISettingsService _settings = settings;
    private string _currentLanguageCode = string.Empty;

    public event Action? LanguageChanged;

    public string CurrentLanguageCode => _currentLanguageCode;

    public void Initialize()
    {
        LocalizationResourceManager.Current.Init(AppResources.ResourceManager);

        var saved = ScreenshotDemoLaunchOptions.IsRequested()
            ? null
            : _settings.Get(SettingsKeys.LanguageCode);
        var culture = string.IsNullOrEmpty(saved)
            ? CultureInfo.CurrentUICulture
            : new CultureInfo(saved);

        ApplyCulture(culture, saved ?? string.Empty);
    }

    public void SetLanguage(string? cultureCode)
    {
        _settings.Set(SettingsKeys.LanguageCode, cultureCode ?? string.Empty);

        var culture = string.IsNullOrEmpty(cultureCode)
            ? CultureInfo.InstalledUICulture
            : new CultureInfo(cultureCode);

        ApplyCulture(culture, cultureCode ?? string.Empty);
    }

    public string GetString(string key)
        => LocalizationResourceManager.Current[key];

    public string GetString(string key, params object[] args)
        => string.Format(GetString(key), args);

    private void ApplyCulture(CultureInfo culture, string code)
    {
        _currentLanguageCode = code;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        LocalizationResourceManager.Current.CurrentCulture = culture;
        LanguageChanged?.Invoke();
    }
}
