namespace Finanzuebersicht.Core.Services;

/// <summary>One Home Screen widget shortcut slot (0–3).</summary>
public sealed record QuickExpenseWidgetPreset(int Slot, string Title, string AmountText)
{
    public bool IsFilled =>
        !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(AmountText);
}

/// <summary>
/// Four user-configurable quick-expense presets for the iOS widget.
/// AmountText is stored invariant (e.g. "3.50").
/// </summary>
public interface IQuickExpenseWidgetPresetStore
{
    /// <summary>Returns exactly four slots (0–3). Missing file → seeded defaults.</summary>
    Task<IReadOnlyList<QuickExpenseWidgetPreset>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists exactly four slots. Empty title+amount clears a slot.</summary>
    Task SaveAsync(IReadOnlyList<QuickExpenseWidgetPreset> presets, CancellationToken cancellationToken = default);
}

public static class QuickExpenseWidgetPresetDefaults
{
    public const int SlotCount = 4;

    /// <summary>First-run defaults matching the previous hardcoded widget presets.</summary>
    public static IReadOnlyList<QuickExpenseWidgetPreset> CreateSeeded() =>
    [
        new(0, "Coffee", "3.50"),
        new(1, "Snack", "5.00"),
        new(2, string.Empty, string.Empty),
        new(3, string.Empty, string.Empty)
    ];

    public static IReadOnlyList<QuickExpenseWidgetPreset> Normalize(IEnumerable<QuickExpenseWidgetPreset>? source)
    {
        var bySlot = (source ?? [])
            .Where(p => p.Slot is >= 0 and < SlotCount)
            .GroupBy(p => p.Slot)
            .ToDictionary(g => g.Key, g => g.Last());

        var result = new QuickExpenseWidgetPreset[SlotCount];
        for (var i = 0; i < SlotCount; i++)
        {
            if (bySlot.TryGetValue(i, out var preset))
            {
                result[i] = new QuickExpenseWidgetPreset(
                    i,
                    preset.Title?.Trim() ?? string.Empty,
                    preset.AmountText?.Trim() ?? string.Empty);
            }
            else
            {
                result[i] = new QuickExpenseWidgetPreset(i, string.Empty, string.Empty);
            }
        }

        return result;
    }
}
