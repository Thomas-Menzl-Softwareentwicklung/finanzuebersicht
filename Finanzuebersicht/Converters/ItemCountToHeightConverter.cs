using System.Collections;
using System.Globalization;

namespace Finanzuebersicht.Converters;

/// <summary>
/// Maps a collection (or int count) to a <see cref="CollectionView"/> HeightRequest
/// when nested inside a ScrollView (MAUI otherwise collapses the list).
/// ConverterParameter: item height in DIPs (invariant culture), default 72.
/// </summary>
public sealed class ItemCountToHeightConverter : IValueConverter
{
    public double DefaultItemHeight { get; set; } = 72;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value switch
        {
            int i => i,
            ICollection c => c.Count,
            IEnumerable e => e.Cast<object>().Count(),
            _ => 0
        };

        var itemHeight = DefaultItemHeight;
        if (parameter is string s &&
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            itemHeight = parsed;
        }
        else if (parameter is double d)
        {
            itemHeight = d;
        }

        return Math.Max(0, count * itemHeight);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
