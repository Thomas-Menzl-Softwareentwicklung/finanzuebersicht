using System.Globalization;

namespace Finanzuebersicht.Core.Services;

/// <summary>
/// Parses user/widget amount text that may use <c>,</c> or <c>.</c> as decimal separator.
/// </summary>
public static class FlexibleAmountParser
{
    /// <summary>
    /// Normalizes to invariant decimal text (dot separator, no group separators), or null if invalid.
    /// </summary>
    public static string? ToInvariantAmountText(string? amountText)
    {
        if (!TryParse(amountText, out var amount))
            return null;

        return amount.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a stored/invariant amount for UI entry (e.g. <c>3,50</c> in de-DE).
    /// </summary>
    public static string ToDisplayAmountText(string? amountText, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (string.IsNullOrWhiteSpace(amountText))
            return string.Empty;

        if (!TryParse(amountText, out var amount))
            return amountText.Trim();

        return amount.ToString("F2", culture);
    }

    public static bool TryParse(string? amountText, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(amountText))
            return false;

        var s = NormalizeInput(amountText);
        if (s.Length == 0)
            return false;

        var lastComma = s.LastIndexOf(',');
        var lastDot = s.LastIndexOf('.');

        if (lastComma >= 0 && lastDot >= 0)
        {
            // Last separator is the decimal separator; the other is thousands.
            if (lastComma > lastDot)
                s = s.Replace(".", "").Replace(',', '.');
            else
                s = s.Replace(",", "");
        }
        else if (lastComma >= 0)
        {
            s = s.Replace(',', '.');
        }

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    /// <summary>
    /// iOS inserts invisible format characters (LRM, bidi isolates) when a minus is typed
    /// into a field that already has a value. Those characters are not visible, and
    /// <see cref="decimal.TryParse"/> rejects them. A new, empty field does not get them.
    /// </summary>
    private static string NormalizeInput(string s)
    {
        var buffer = new char[s.Length];
        var count = 0;
        foreach (var c in s.Trim())
        {
            var category = char.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Format or UnicodeCategory.SpaceSeparator or UnicodeCategory.Control)
                continue;

            buffer[count++] = category == UnicodeCategory.DashPunctuation || c == '\u2212'
                ? '-'
                : c;
        }

        return new string(buffer, 0, count);
    }
}
