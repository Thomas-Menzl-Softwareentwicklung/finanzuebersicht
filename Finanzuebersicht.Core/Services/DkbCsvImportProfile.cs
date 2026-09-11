namespace Finanzuebersicht.Core.Services;

public static class DkbCsvImportProfile
{
    public static CsvImportProfile Instance { get; } = new()
    {
        Id = "builtin-dkb",
        IsBuiltIn = true,
        Delimiter = ';',
        Headers =
        [
            "Buchungsdatum",
            "Wertstellung",
            "Status",
            "Zahlungspflichtige*r",
            "Zahlungsempfänger*in",
            "Verwendungszweck",
            "Umsatztyp",
            "IBAN",
            "Betrag (€)",
            "Gläubiger-ID",
            "Mandatsreferenz",
            "Kundenreferenz"
        ],
        Columns = new CsvColumnMapping
        {
            Date = "Buchungsdatum",
            Amount = "Betrag (€)",
            Title = "Zahlungsempfänger*in",
            Purpose = "Verwendungszweck",
            AmountSign = "Umsatztyp",
            Iban = "IBAN"
        },
        DateFormat = "dd.MM.yy",
        DecimalStyle = CsvDecimalStyle.Comma
    };
}
