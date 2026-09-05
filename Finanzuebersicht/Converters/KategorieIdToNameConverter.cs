using System.Globalization;

namespace Finanzuebersicht.Converters;

/// <summary>
/// Converts a KategorieId (values[0]) to its display name using a CategoryNameMap dictionary (values[1]).
/// Using IMultiValueConverter avoids a static shared cache.
/// </summary>
public class KategorieIdToNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 1 || values[0] is not string kategorieId || string.IsNullOrEmpty(kategorieId))
            return string.Empty;

        if (values.Length >= 2 && values[1] is IDictionary<string, string> nameMap
            && nameMap.TryGetValue(kategorieId, out var name))
            return name;

        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
