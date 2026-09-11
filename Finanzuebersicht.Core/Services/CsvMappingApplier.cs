using System.Globalization;

namespace Finanzuebersicht.Core.Services;

public static class CsvMappingApplier
{
    private static readonly HashSet<string> IncomeTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "Eingang", "Haben", "Credit", "Einnahme", "+"
    };

    private static readonly HashSet<string> ExpenseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ausgang", "Soll", "Debit", "Ausgabe", "-"
    };

    public static IReadOnlyList<TransactionDto> Apply(CsvTable table, CsvImportProfile profile)
    {
        var cols = profile.Columns;
        var dateIdx = ResolveColumnIndex(table.Headers, cols.Date);
        var amountIdx = ResolveColumnIndex(table.Headers, cols.Amount);
        var titleIdx = ResolveColumnIndex(table.Headers, cols.Title);
        var purposeIdx = ResolveColumnIndex(table.Headers, cols.Purpose);
        var amountSignIdx = ResolveColumnIndex(table.Headers, cols.AmountSign);
        var ibanIdx = ResolveColumnIndex(table.Headers, cols.Iban);

        if (cols.Date is not null && dateIdx < 0) return [];
        if (cols.Amount is not null && amountIdx < 0) return [];
        if (cols.Title is not null && titleIdx < 0) return [];
        if (cols.Purpose is not null && purposeIdx < 0) return [];
        if (cols.AmountSign is not null && amountSignIdx < 0) return [];
        if (cols.Iban is not null && ibanIdx < 0) return [];

        var culture = profile.DecimalStyle == CsvDecimalStyle.Comma
            ? CultureInfo.GetCultureInfo("de-DE")
            : CultureInfo.GetCultureInfo("en-US");

        var result = new List<TransactionDto>();
        foreach (var row in table.DataRows)
        {
            if (!TryParseDate(GetCell(row, dateIdx), profile.DateFormat, out var date))
                continue;
            if (!TryParseAmount(GetCell(row, amountIdx), culture, out var amount))
                continue;

            if (amountSignIdx >= 0)
                amount = ApplySign(amount, GetCell(row, amountSignIdx));

            result.Add(new TransactionDto
            {
                Buchungsdatum = date,
                Wertstellung = date,
                Zahlungsempfaenger = titleIdx >= 0 ? GetCell(row, titleIdx) : string.Empty,
                Verwendungszweck = purposeIdx >= 0 ? GetCell(row, purposeIdx) : string.Empty,
                IBAN = ibanIdx >= 0 ? GetCell(row, ibanIdx) : string.Empty,
                Betrag = amount
            });
        }

        return result;
    }

    private static int ResolveColumnIndex(IReadOnlyList<string> headers, string? mappedName)
    {
        if (mappedName is null)
            return -1;

        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i] == mappedName)
                return i;
        }

        var normalized = CsvImportFingerprint.Normalize(mappedName);
        for (var i = 0; i < headers.Count; i++)
        {
            if (CsvImportFingerprint.Normalize(headers[i]) == normalized)
                return i;
        }

        return -1;
    }

    private static string GetCell(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index].Trim('"') : string.Empty;

    private static bool TryParseDate(string value, string format, out DateTime date)
    {
        value = value.Trim('"', ' ');
        if (string.IsNullOrWhiteSpace(value))
        {
            date = default;
            return false;
        }

        return DateTime.TryParseExact(
            value,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private static bool TryParseAmount(string value, CultureInfo culture, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim('"', ' ');
        value = value.Replace("€", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);

        return decimal.TryParse(
            value,
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            culture,
            out amount);
    }

    private static decimal ApplySign(decimal amount, string signToken)
    {
        signToken = signToken.Trim();
        if (IncomeTokens.Contains(signToken))
            return Math.Abs(amount);
        if (ExpenseTokens.Contains(signToken))
            return -Math.Abs(amount);
        return amount;
    }
}
