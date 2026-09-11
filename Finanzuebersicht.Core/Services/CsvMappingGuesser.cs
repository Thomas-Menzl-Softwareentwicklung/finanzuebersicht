namespace Finanzuebersicht.Core.Services;

public static class CsvMappingGuesser
{
    private static readonly string[] PreferredDateTokens =
        ["Buchungsdatum", "Buchungstag", "Datum", "Date"];

    private static readonly string[] FallbackDateTokens = ["Wertstellung", "Valuta"];

    private static readonly string[] AmountTokens = ["Betrag", "Amount", "Umsatz"];

    private static readonly string[] TitleTokens =
        ["Zahlungsempfänger", "Empfänger", "Payee", "Auftraggeber"];

    private static readonly string[] PurposeTokens =
        ["Verwendungszweck", "Buchungstext", "Beschreibung", "Purpose", "Text"];

    private static readonly string[] AmountSignTokens =
        ["Umsatztyp", "Soll/Haben", "Debit/Credit", "S/H", "Typ"];

    private static readonly string[] IbanTokens = ["IBAN"];

    public static CsvColumnMapping Guess(CsvTable table)
    {
        var used = new HashSet<string>();
        return new CsvColumnMapping
        {
            Date = FindHeader(table.Headers, used, PreferredDateTokens)
                   ?? FindHeader(table.Headers, used, FallbackDateTokens),
            Amount = FindHeader(table.Headers, used, AmountTokens, ExcludeKontostand),
            Title = FindHeader(table.Headers, used, TitleTokens),
            Purpose = FindHeader(table.Headers, used, PurposeTokens),
            AmountSign = FindHeader(table.Headers, used, AmountSignTokens),
            Iban = FindHeader(table.Headers, used, IbanTokens)
        };
    }

    private static string? FindHeader(
        IReadOnlyList<string> headers,
        HashSet<string> used,
        string[] tokens,
        Func<string, bool>? extraFilter = null)
    {
        foreach (var token in tokens.OrderByDescending(t => t.Length))
        {
            foreach (var header in headers)
            {
                if (used.Contains(header))
                    continue;
                if (extraFilter is not null && !extraFilter(header))
                    continue;
                if (MatchesToken(header, token))
                {
                    used.Add(header);
                    return header;
                }
            }
        }

        return null;
    }

    private static bool ExcludeKontostand(string header) =>
        !NormalizeHeader(header).Contains("kontostand", StringComparison.Ordinal);

    internal static bool MatchesToken(string header, string token)
    {
        var normalizedHeader = NormalizeHeader(header);
        var normalizedToken = NormalizeHeader(token);

        if (normalizedHeader == normalizedToken)
            return true;

        if (normalizedHeader == normalizedToken + "in"
            || normalizedHeader == normalizedToken + "/in")
            return true;

        return false;
    }

    internal static string NormalizeHeader(string header)
    {
        var s = header.Trim().ToLowerInvariant();
        s = s.Replace("*", "");
        s = s.Replace("(€)", "");
        s = s.Replace("€", "");
        return s.Trim();
    }
}
