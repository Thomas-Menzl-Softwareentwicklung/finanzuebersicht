using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Tests.TestHelpers;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels.Settings;

public class AppearanceViewModelTests
{
    [Fact]
    public void Constructor_LoadsThemeFromSettings()
    {
        using var settingsScope = new SettingsScope(nameof(AppearanceViewModelTests), (SettingsKeys.Theme, ThemeValues.Dark));
        var themeService = Substitute.For<IThemeService>();
        var localizationService = CreateLocalizationService();

        var sut = CreateSut(settingsScope.Settings, themeService, localizationService);

        Assert.Equal(2, sut.SelectedThemeIndex);
    }

    [Fact]
    public void Constructor_LoadsLanguageFromLocalizationService()
    {
        using var settingsScope = new SettingsScope(nameof(AppearanceViewModelTests));
        var themeService = Substitute.For<IThemeService>();
        var localizationService = CreateLocalizationService("en");

        var sut = CreateSut(settingsScope.Settings, themeService, localizationService);

        Assert.Equal(2, sut.SelectedLanguageIndex);
    }

    [Fact]
    public void SetTheme_UpdatesIndexAndPersists()
    {
        using var settingsScope = new SettingsScope(nameof(AppearanceViewModelTests));
        var themeService = Substitute.For<IThemeService>();
        var localizationService = CreateLocalizationService();
        var sut = CreateSut(settingsScope.Settings, themeService, localizationService);

        sut.SetThemeCommand.Execute("1");

        Assert.Equal(1, sut.SelectedThemeIndex);
        Assert.Equal(ThemeValues.Light, settingsScope.Settings.Get(SettingsKeys.Theme));
        themeService.Received(1).Apply(ThemeValues.Light);
    }

    [Fact]
    public void OnSelectedThemeIndexChanged_AppliesTheme()
    {
        using var settingsScope = new SettingsScope(nameof(AppearanceViewModelTests));
        var themeService = Substitute.For<IThemeService>();
        var localizationService = CreateLocalizationService();
        var sut = CreateSut(settingsScope.Settings, themeService, localizationService);

        sut.SelectedThemeIndex = 2;

        Assert.Equal(ThemeValues.Dark, settingsScope.Settings.Get(SettingsKeys.Theme));
        themeService.Received(1).Apply(ThemeValues.Dark);
    }

    [Fact]
    public void SetLanguage_UpdatesIndex()
    {
        using var settingsScope = new SettingsScope(nameof(AppearanceViewModelTests));
        var themeService = Substitute.For<IThemeService>();
        var localizationService = CreateLocalizationService();
        var sut = CreateSut(settingsScope.Settings, themeService, localizationService);

        sut.SetLanguageCommand.Execute("2");

        Assert.Equal(2, sut.SelectedLanguageIndex);
        localizationService.Received(1).SetLanguage("en");
    }

    [Fact]
    public void SetCurrency_UpdatesIndex()
    {
        using var settingsScope = new SettingsScope(nameof(AppearanceViewModelTests));
        var themeService = Substitute.For<IThemeService>();
        var localizationService = CreateLocalizationService();
        var displayCurrency = CreateDisplayCurrencyService();
        var sut = CreateSut(settingsScope.Settings, themeService, localizationService, displayCurrency);

        sut.SetCurrencyCommand.Execute("2");

        Assert.Equal(2, sut.SelectedCurrencyIndex);
        displayCurrency.Received(1).SelectedIndex = 2;
    }

    [Fact]
    public async Task RequestAdditionalLanguage_OpensMailto()
    {
        using var settingsScope = new SettingsScope(nameof(AppearanceViewModelTests));
        var browser = Substitute.For<IExternalBrowser>();
        var sut = CreateSut(
            settingsScope.Settings,
            Substitute.For<IThemeService>(),
            CreateLocalizationService(),
            browser: browser);

        await sut.RequestAdditionalLanguageCommand.ExecuteAsync(null);

        await browser.Received(1).OpenAsync(new Uri(SupportContact.LanguageRequestMailto));
    }

    private static AppearanceViewModel CreateSut(
        ISettingsService settings,
        IThemeService themeService,
        ILocalizationService localizationService,
        IDisplayCurrencyService? displayCurrency = null,
        IExternalBrowser? browser = null,
        IDialogService? dialog = null)
    {
        return new AppearanceViewModel(
            settings,
            themeService,
            localizationService,
            displayCurrency ?? CreateDisplayCurrencyService(),
            browser ?? Substitute.For<IExternalBrowser>(),
            dialog ?? Substitute.For<IDialogService>());
    }

    private static IDisplayCurrencyService CreateDisplayCurrencyService(int selectedIndex = 0)
    {
        var displayCurrency = Substitute.For<IDisplayCurrencyService>();
        displayCurrency.SelectedIndex.Returns(selectedIndex);
        return displayCurrency;
    }

    private static ILocalizationService CreateLocalizationService(string currentLanguageCode = "")
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.CurrentLanguageCode.Returns(currentLanguageCode);
        localizationService.GetString(Arg.Any<string>()).Returns(ci => (string)ci[0]!);
        return localizationService;
    }
}
