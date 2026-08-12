using System.Globalization;

namespace Finanzuebersicht.Converters;

public class KategorieIdToColorConverter : IMultiValueConverter
{
    private static readonly Color Fallback = Color.FromArgb("#8E8E93");

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 1 || values[0] is not string kategorieId || string.IsNullOrEmpty(kategorieId))
            return Fallback;

        if (values.Length >= 2 && values[1] is IDictionary<string, string> colorMap
            && colorMap.TryGetValue(kategorieId, out var hex)
            && !string.IsNullOrWhiteSpace(hex))
        {
            try { return Color.FromArgb(hex); }
            catch { return Fallback; }
        }

        return Fallback;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
