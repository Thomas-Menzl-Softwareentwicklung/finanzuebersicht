using System.Globalization;
using System.Text.RegularExpressions;

namespace Finanzuebersicht.Core.Services;

public static class CsvFormatDetector
{
    private static readonly string[] DateFormats =
        ["dd.MM.yyyy", "dd.MM.yy", "yyyy-MM-dd", "dd/MM/yyyy", "d.M.yyyy"];

    private static readonly CultureInfo[] DateCultures =
        [CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("de-DE")];

    private static readonly Regex DecimalSeparatorPattern = new(
        @"([.,])(\d{1,2})(?!\d)",
        RegexOptions.Compiled);

    public static string DetectDateFormat(IEnumerable<string> samples)
    {
        var nonEmpty = samples.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (nonEmpty.Count == 0)
            return "dd.MM.yyyy";

        var bestFormat = "";
        var bestCount = 0;

        foreach (var format in DateFormats)
        {
            var count = nonEmpty.Count(s => ParsesAsDate(s, format));
            if (count > bestCount)
            {
                bestCount = count;
                bestFormat = format;
            }
        }

        if (bestCount > nonEmpty.Count / 2)
            return bestFormat;

        return "dd.MM.yyyy";
    }

    public static CsvDecimalStyle DetectDecimalStyle(IEnumerable<string> samples)
    {
        var commaCount = 0;
        var pointCount = 0;

        foreach (var sample in samples)
        {
            if (string.IsNullOrWhiteSpace(sample))
                continue;

            var mark = DetectDecimalMark(sample);
            if (mark == ',')
                commaCount++;
            else if (mark == '.')
                pointCount++;
        }

        if (pointCount > commaCount)
            return CsvDecimalStyle.Point;

        return CsvDecimalStyle.Comma;
    }

    private static bool ParsesAsDate(string value, string format)
    {
        foreach (var culture in DateCultures)
        {
            if (DateTime.TryParseExact(
                    value.Trim(),
                    format,
                    culture,
                    DateTimeStyles.None,
                    out _))
                return true;
        }

        return false;
    }

    private static char? DetectDecimalMark(string value)
    {
        var matches = DecimalSeparatorPattern.Matches(value);
        if (matches.Count == 0)
            return null;

        var last = matches[^1];
        return last.Groups[1].Value[0];
    }
}
