using System.Globalization;

namespace Finanzuebersicht.Infrastructure.Services;

public sealed class DisplayCurrencyService : IDisplayCurrencyService
{
    private static readonly string[] Codes = ["EUR", "CHF", "USD", "GBP"];

    private static readonly Dictionary<string, string> CultureByCode = new(StringComparer.Ordinal)
    {
        ["EUR"] = "de-DE",
        ["CHF"] = "de-CH",
        ["USD"] = "en-US",
        ["GBP"] = "en-GB",
    };

    private readonly ISettingsService _settings;
    private string _currencyCode;

    public DisplayCurrencyService(ISettingsService settings)
    {
        _settings = settings;
        _currencyCode = LoadCurrencyCode();
        CurrencyCulture.Initialize(this);
    }

    public IReadOnlyList<string> SupportedCodes => Codes;

    public string CurrencyCode => _currencyCode;

    public CultureInfo FormatCulture => ResolveCulture(_currencyCode);

    public event Action? Changed;

    public int SelectedIndex
    {
        get
        {
            var index = Array.IndexOf(Codes, _currencyCode);
            return index >= 0 ? index : 0;
        }
        set
        {
            if (value < 0 || value >= Codes.Length)
                return;

            SetCurrencyCode(Codes[value]);
        }
    }

    private void SetCurrencyCode(string code)
    {
        if (!CultureByCode.ContainsKey(code))
            code = "EUR";

        if (string.Equals(_currencyCode, code, StringComparison.Ordinal))
            return;

        _currencyCode = code;
        _settings.Set(SettingsKeys.DisplayCurrency, code);
        Changed?.Invoke();
    }

    private string LoadCurrencyCode()
    {
        var code = _settings.Get(SettingsKeys.DisplayCurrency, "");
        if (string.IsNullOrEmpty(code))
            code = _settings.Get(SettingsKeys.Currency, "EUR");

        code = code.Trim().ToUpperInvariant();
        return CultureByCode.ContainsKey(code) ? code : "EUR";
    }

    private static CultureInfo ResolveCulture(string code)
    {
        if (!CultureByCode.TryGetValue(code, out var cultureName))
            cultureName = "de-DE";

        return CultureInfo.GetCultureInfo(cultureName);
    }
}
