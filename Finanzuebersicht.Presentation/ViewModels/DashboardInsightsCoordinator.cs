using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public sealed record DashboardMonthBudgetTotals(
    decimal BudgetGesamt,
    decimal BudgetVerbraucht,
    decimal BudgetRest,
    bool ShowBudgetTagesbudget,
    decimal BudgetTagesbudget);

public sealed record DashboardInsightTexts(
    string KontenCompactSummary,
    string BudgetInsightDetail,
    string DueRecurringCompactDetail);

/// <summary>
/// Budget totals and compact insight row text for the dashboard.
/// </summary>
public sealed class DashboardInsightsCoordinator(ILocalizationService localizationService)
{
    private readonly ILocalizationService _loc = localizationService;

    public static IEnumerable<BudgetHintSummary> GetBudgetWarnings(IEnumerable<BudgetHintSummary> budgetHinweise) =>
        budgetHinweise.Where(b => b.IstWarnung || b.IstAusgeschoepft);

    public DashboardMonthBudgetTotals ComputeMonthBudgetTotals(IReadOnlyList<BudgetHintSummary> budgetHinweise)
    {
        var budgetGesamt = budgetHinweise.Sum(b => b.BudgetBetrag);
        var budgetVerbraucht = budgetHinweise.Sum(b => b.Verbrauch);
        var budgetRest = budgetHinweise.Sum(b => b.Restbudget);
        var showBudgetTagesbudget = budgetHinweise.Any(b => b.ZeigeTagesbudget);
        var remainingDays = budgetHinweise.FirstOrDefault(b => b.IstAktuellerMonat)?.VerbleibendeTage ?? 0;
        var budgetTagesbudget = remainingDays > 0
            ? budgetHinweise.Sum(b => b.RestbudgetPositiv) / remainingDays
            : 0;

        return new DashboardMonthBudgetTotals(
            budgetGesamt,
            budgetVerbraucht,
            budgetRest,
            showBudgetTagesbudget,
            budgetTagesbudget);
    }

    public DashboardInsightTexts BuildInsightTexts(
        IReadOnlyList<AccountOverviewItem> kontenUebersicht,
        IReadOnlyList<BudgetHintSummary> budgetHinweise,
        string? firstDueRecurringTitle)
    {
        var culture = CurrencyCulture.Instance;
        var kontenCompactSummary = string.Join(" · ",
            kontenUebersicht.Take(3).Select(k => $"{k.Name} {k.Saldo.ToString("C", culture)}"));

        var budgetWarnings = GetBudgetWarnings(budgetHinweise).ToList();
        var budgetInsightDetail = budgetWarnings.Count switch
        {
            0 => string.Empty,
            1 => budgetWarnings[0].CategoryName,
            _ => _loc.GetString(ResourceKeys.Fmt_BudgetUeberLimitCount, budgetWarnings.Count)
        };

        return new DashboardInsightTexts(
            kontenCompactSummary,
            budgetInsightDetail,
            firstDueRecurringTitle ?? string.Empty);
    }

    public bool ShowBudgetInsightRow(bool showInsightRows, IEnumerable<BudgetHintSummary> budgetHinweise) =>
        showInsightRows && GetBudgetWarnings(budgetHinweise).Any();
}
