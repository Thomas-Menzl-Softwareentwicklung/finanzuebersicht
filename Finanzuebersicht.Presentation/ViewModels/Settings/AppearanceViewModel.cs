using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class AppearanceViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _loc;
    private readonly IDisplayCurrencyService _displayCurrency;
    private readonly IExternalBrowser _externalBrowser;
    private readonly IDialogService _dialogService;
    private readonly ILogger<AppearanceViewModel>? _logger;

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
        IDisplayCurrencyService displayCurrency,
        IExternalBrowser externalBrowser,
        IDialogService dialogService,
        ILogger<AppearanceViewModel>? logger = null)
    {
        _settings = settings;
        _themeService = themeService;
        _loc = localizationService;
        _displayCurrency = displayCurrency;
        _externalBrowser = externalBrowser;
        _dialogService = dialogService;
        _logger = logger;

        var theme = _settings.Get(SettingsKeys.Theme, ThemeValues.System);
        selectedThemeIndex = theme switch
        {
            ThemeValues.Light => 1,
            ThemeValues.Dark => 2,
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
            1 => ThemeValues.Light,
            2 => ThemeValues.Dark,
            _ => ThemeValues.System
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

    [RelayCommand]
    private async Task RequestAdditionalLanguageAsync()
    {
        try
        {
            var subject = _loc.GetString(ResourceKeys.Mail_SubjectSpracheAnfragen);
            await _externalBrowser.OpenAsync(new Uri(SupportContact.CreateLanguageRequestMailto(subject)));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to open language request mailto");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                ex.Message,
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }
}
