using System.Globalization;
using System.Text;

namespace Finanzuebersicht.Core.Services;

public static class CsvTableReader
{
    private static readonly char[] DelimiterCandidates = [';', ',', '\t'];
    private static readonly string[] HeaderKeywords =
    [
        "datum", "date", "buchung", "betrag", "amount", "verwendung", "iban", "umsatz"
    ];

    static CsvTableReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static bool TryRead(byte[] bytes, out CsvTable? table, out string? errorKey)
    {
        table = null;
        errorKey = null;

        if (bytes == null || bytes.Length == 0)
        {
            errorKey = ImportMessageKeys.CsvNotTabular;
            return false;
        }

        var (content, encodingName) = Decode(bytes);
        if (string.IsNullOrWhiteSpace(content))
        {
            errorKey = ImportMessageKeys.CsvNotTabular;
            return false;
        }

        var delimiter = DetectDelimiter(content);
        var records = ParseCsv(content, delimiter);
        if (records.Count == 0)
        {
            errorKey = ImportMessageKeys.CsvNotTabular;
            return false;
        }

        var headerRowIndex = FindHeaderRowIndex(records);
        if (headerRowIndex < 0)
        {
            errorKey = ImportMessageKeys.CsvNotTabular;
            return false;
        }

        var headers = records[headerRowIndex];
        var dataRows = records.Skip(headerRowIndex + 1)
            .Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c)))
            .ToList();

        table = new CsvTable
        {
            Delimiter = delimiter,
            EncodingName = encodingName,
            HeaderRowIndex = headerRowIndex,
            Headers = headers,
            DataRows = dataRows,
            AllRows = records
        };
        return true;
    }

    private static (string Content, string EncodingName) Decode(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return (Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), "utf-8");

        try
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return (utf8.GetString(bytes), "utf-8");
        }
        catch (DecoderFallbackException)
        {
            return (Encoding.GetEncoding(1252).GetString(bytes), "windows-1252");
        }
    }

    private static char DetectDelimiter(string content)
    {
        char best = ';';
        var bestScore = -1;

        foreach (var candidate in DelimiterCandidates)
        {
            var records = ParseCsv(content, candidate);
            var nonEmpty = records
                .Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c)))
                .Take(15)
                .ToList();
            if (nonEmpty.Count == 0)
                continue;

            var modeGroup = nonEmpty
                .GroupBy(r => r.Length)
                .Where(g => g.Key >= 2)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            if (modeGroup == null)
                continue;

            var score = modeGroup.Count();
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static int FindHeaderRowIndex(List<string[]> records)
    {
        for (var i = 0; i < records.Count; i++)
        {
            if (IsHeaderRow(records[i]))
                return i;
        }

        if (records.Count > 0 && CountNonEmptyCells(records[0]) >= 2)
            return 0;

        return -1;
    }

    private static bool IsHeaderRow(string[] row)
    {
        var cells = row
            .Where(c => !string.IsNullOrWhiteSpace(c.Trim('"')))
            .ToList();
        if (cells.Count < 3)
            return false;

        if (cells.Any(ContainsHeaderKeyword))
            return true;

        var nonNumeric = cells.Count(c => !IsLikelyNumeric(c));
        return nonNumeric > cells.Count / 2;
    }

    private static bool ContainsHeaderKeyword(string cell)
    {
        var lower = cell.Trim().Trim('"').ToLowerInvariant();
        return HeaderKeywords.Any(k => lower.Contains(k, StringComparison.Ordinal));
    }

    private static bool IsLikelyNumeric(string cell)
    {
        var s = cell.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(s))
            return false;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return true;
        if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out _))
            return true;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            return true;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.GetCultureInfo("de-DE"), out _))
            return true;

        return false;
    }

    private static int CountNonEmptyCells(string[] row) =>
        row.Count(c => !string.IsNullOrWhiteSpace(c));

    private static List<string[]> ParseCsv(string content, char sep)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var cur = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                {
                    cur.Append('"');
                    i++;
                    continue;
                }
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && ch == sep)
            {
                fields.Add(cur.ToString());
                cur.Clear();
                continue;
            }

            if (!inQuotes && (ch == '\r' || ch == '\n'))
            {
                if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    i++;
                fields.Add(cur.ToString());
                cur.Clear();
                records.Add(fields.ToArray());
                fields.Clear();
                continue;
            }

            cur.Append(ch);
        }

        if (cur.Length > 0 || fields.Count > 0)
        {
            fields.Add(cur.ToString());
            records.Add([.. fields]);
        }

        return records;
    }
}
