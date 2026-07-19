using Finanzuebersicht.Infrastructure.Services;
using NSubstitute;

namespace Finanzuebersicht.Tests.Core.Services;

public class DisplayCurrencyServiceTests
{
    [Fact]
    public void DefaultCurrency_IsEur_WithDeDeCulture()
    {
        var settings = Substitute.For<ISettingsService>();
        settings.Get(SettingsKeys.DisplayCurrency, "").Returns("");
        settings.Get(SettingsKeys.Currency, "EUR").Returns("EUR");

        var service = new DisplayCurrencyService(settings);

        Assert.Equal("EUR", service.CurrencyCode);
        Assert.Equal("de-DE", service.FormatCulture.Name);
        Assert.Equal("de-DE", CurrencyCulture.Instance.Name);
    }

    [Fact]
    public void LegacyCurrencyKey_IsUsedWhenDisplayCurrencyMissing()
    {
        var settings = Substitute.For<ISettingsService>();
        settings.Get(SettingsKeys.DisplayCurrency, "").Returns("");
        settings.Get(SettingsKeys.Currency, "EUR").Returns("USD");

        var service = new DisplayCurrencyService(settings);

        Assert.Equal("USD", service.CurrencyCode);
        Assert.Equal("en-US", service.FormatCulture.Name);
    }

    [Fact]
    public void SetCurrency_PersistsAndRaisesChanged()
    {
        var settings = Substitute.For<ISettingsService>();
        settings.Get(SettingsKeys.DisplayCurrency, "").Returns("");
        settings.Get(SettingsKeys.Currency, "EUR").Returns("EUR");

        var service = new DisplayCurrencyService(settings);
        var changed = false;
        service.Changed += () => changed = true;

        service.SelectedIndex = 2;

        Assert.Equal("USD", service.CurrencyCode);
        Assert.Equal("en-US", CurrencyCulture.Instance.Name);
        settings.Received(1).Set(SettingsKeys.DisplayCurrency, "USD");
        Assert.True(changed);
    }

    [Theory]
    [InlineData("usd", "USD", "en-US")]
    [InlineData(" usd ", "USD", "en-US")]
    [InlineData("chf", "CHF", "de-CH")]
    public void LoadCurrencyCode_NormalizesStoredValue(string stored, string expectedCode, string expectedCulture)
    {
        var settings = Substitute.For<ISettingsService>();
        settings.Get(SettingsKeys.DisplayCurrency, "").Returns(stored);

        var service = new DisplayCurrencyService(settings);

        Assert.Equal(expectedCode, service.CurrencyCode);
        Assert.Equal(expectedCulture, service.FormatCulture.Name);
    }

    [Theory]
    [InlineData(0, "EUR", "de-DE")]
    [InlineData(1, "CHF", "de-CH")]
    [InlineData(2, "USD", "en-US")]
    [InlineData(3, "GBP", "en-GB")]
    public void SelectedIndex_MapsToExpectedCulture(int index, string code, string culture)
    {
        var settings = Substitute.For<ISettingsService>();
        settings.Get(SettingsKeys.DisplayCurrency, "").Returns("");
        settings.Get(SettingsKeys.Currency, "EUR").Returns("EUR");

        var service = new DisplayCurrencyService(settings);
        service.SelectedIndex = index;

        Assert.Equal(code, service.CurrencyCode);
        Assert.Equal(culture, service.FormatCulture.Name);
    }
}
