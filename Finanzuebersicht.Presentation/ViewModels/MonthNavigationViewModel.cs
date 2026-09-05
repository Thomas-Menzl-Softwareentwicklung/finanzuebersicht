using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Finanzuebersicht.ViewModels;

/// <summary>
/// Basisklasse für ViewModels mit monatlicher Navigation (Vor/Zurück).
/// Abgeleitete Klassen implementieren <see cref="OnMonthChangedAsync"/> für den Reload.
/// </summary>
public abstract partial class MonthNavigationViewModel : ObservableObject
{
    [ObservableProperty]
    private string monatAnzeige = string.Empty;

    public DateTime AktuellerMonat { get; private set; }

    protected MonthNavigationViewModel(Finanzuebersicht.Core.Services.IClock? clock = null)
    {
        var c = clock ?? Finanzuebersicht.Core.Services.SystemClock.Instance;
        AktuellerMonat = new(c.Today.Year, c.Today.Month, 1);
        UpdateMonatAnzeige();
    }

    [RelayCommand]
    private async Task NextMonth()
    {
        AktuellerMonat = AktuellerMonat.AddMonths(1);
        UpdateMonatAnzeige();
        await OnMonthChangedAsync();
    }

    [RelayCommand]
    private async Task PreviousMonth()
    {
        AktuellerMonat = AktuellerMonat.AddMonths(-1);
        UpdateMonatAnzeige();
        await OnMonthChangedAsync();
    }

    protected abstract Task OnMonthChangedAsync();

    protected void UpdateMonatAnzeige()
    {
        MonatAnzeige = AktuellerMonat.ToString("MMMM yyyy",
            System.Globalization.CultureInfo.CurrentCulture);
        OnMonatAnzeigeUpdated();
    }

    protected virtual void OnMonatAnzeigeUpdated() { }
}
