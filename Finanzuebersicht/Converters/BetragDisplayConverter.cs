using System.Globalization;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Converters;

public class BetragDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ci = CurrencyCulture.Instance;

        if (value is Transaction t)
        {
            var abs = Math.Abs(t.Betrag);
            var formatted = abs.ToString("C", ci);
            var sign = t.Typ == TransactionType.Einnahme ? "+" : "-";
            return $"{sign}{formatted}";
        }

        if (value is RecurringTransaction recurring)
        {
            var formatted = Math.Abs(recurring.Betrag).ToString("C", ci);
            var sign = recurring.Typ == TransactionType.Einnahme ? "+" : "-";
            return $"{sign}{formatted}";
        }

        if (value is TransactionTemplate tpl)
        {
            var abs = Math.Abs(tpl.Betrag);
            var formatted = abs.ToString("C", ci);
            var sign = tpl.Typ == TransactionType.Einnahme ? "+" : "-";
            return $"{sign}{formatted}";
        }

        if (value is decimal dec)
        {
            var abs = Math.Abs(dec);
            var formatted = abs.ToString("C", ci);
            var sign = dec >= 0 ? "+" : "-";
            return $"{sign}{formatted}";
        }

        if (value is double d)
        {
            var abs = Math.Abs(d);
            var formatted = abs.ToString("C", ci);
            var sign = d >= 0 ? "+" : "-";
            return $"{sign}{formatted}";
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
