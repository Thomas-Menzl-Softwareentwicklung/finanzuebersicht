using System.Globalization;
using System.Text;

namespace Finanzuebersicht.Core.Services.ScreenshotDemo;

/// <summary>Fictional bank export consumed by the real CSV preview flow in screenshot sessions.</summary>
public static class ScreenshotDemoCsv
{
    public static byte[] Create(IClock clock, CultureInfo culture)
    {
        var english = culture.TwoLetterISOLanguageName == "en";
        var date = clock.Today.ToString("dd.MM.yy", CultureInfo.InvariantCulture);
        var rows = new[]
        {
            ("REWE", english ? "Weekly groceries" : "Wocheneinkauf", "-84,60"),
            ("Netflix", english ? "Monthly subscription" : "Monatsabo", "-12,99"),
            (english ? "Pharmacy" : "Apotheke", english ? "Health" : "Gesundheit", "-18,50"),
            ("Shell", english ? "Fuel" : "Tanken", "-52,40"),
            (english ? "Salary" : "Gehalt", english ? "Salary payment" : "Gehaltszahlung", "4200,00")
        };
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(';', DkbCsvImportProfile.Instance.Headers));
        foreach (var (payee, purpose, amount) in rows)
            csv.AppendLine($"{date};{date};Gebucht;{payee};{payee};{purpose};;DE00000000000000000000;{amount};;;");
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
