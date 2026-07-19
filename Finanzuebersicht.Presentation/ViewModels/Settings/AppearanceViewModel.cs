using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Finanzuebersicht.ViewModels;

public partial class AppearanceViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _loc;
    private readonly IDisplayCurrencyService _displayCurrency;

    [ObservableProperty]
    private int selectedThemeIndex;

    [ObservableProperty]
    private int selectedLanguageIndex;

    [ObservableProperty]
    private int selectedCurrencyIndex;

    public AppearanceViewModel(
        ISettingsService settings,
        IThemeService themeService,
        ILocalizationService localizationService,
        IDisplayCurrencyService displayCurrency)
    {
        _settings = settings;
        _themeService = themeService;
        _loc = localizationService;
        _displayCurrency = displayCurrency;

        var theme = _settings.Get(SettingsKeys.Theme, "System");
        selectedThemeIndex = theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        var lang = _loc.CurrentLanguageCode;
        selectedLanguageIndex = lang switch
        {
            "de" => 1,
            "en" => 2,
            _ => 0
        };

        selectedCurrencyIndex = _displayCurrency.SelectedIndex;
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var themeKey = value switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };

        _settings.Set(SettingsKeys.Theme, themeKey);
        _themeService.Apply(themeKey);
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        var code = value switch
        {
            1 => "de",
            2 => "en",
            _ => string.Empty
        };

        _loc.SetLanguage(string.IsNullOrEmpty(code) ? null : code);
    }

    partial void OnSelectedCurrencyIndexChanged(int value)
    {
        _displayCurrency.SelectedIndex = value;
    }

    [RelayCommand]
    private void SetTheme(string indexStr)
    {
        if (int.TryParse(indexStr, out var idx))
        {
            SelectedThemeIndex = idx;
        }
    }

    [RelayCommand]
    private void SetLanguage(string indexStr)
    {
        if (int.TryParse(indexStr, out var idx))
        {
            SelectedLanguageIndex = idx;
        }
    }

    [RelayCommand]
    private void SetCurrency(string indexStr)
    {
        if (int.TryParse(indexStr, out var idx))
        {
            SelectedCurrencyIndex = idx;
        }
    }
}
