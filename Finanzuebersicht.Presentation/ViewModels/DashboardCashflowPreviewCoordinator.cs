using Finanzuebersicht.Application.UseCases.Dashboard;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public sealed record DashboardCashflowPreviewSnapshot(
    decimal NetAmount,
    decimal ProjectedIncome,
    decimal ProjectedExpenses,
    int NotableDays,
    string SummaryText);

/// <summary>
/// 30-day cashflow preview for the dashboard (Pro-gated).
/// </summary>
public sealed class DashboardCashflowPreviewCoordinator(
    LoadCashflowOutlookUseCase loadCashflowOutlookUseCase,
    ILocalizationService localizationService,
    ILogger<DashboardCashflowPreviewCoordinator>? logger = null)
{
    private readonly LoadCashflowOutlookUseCase _loadCashflowOutlookUseCase = loadCashflowOutlookUseCase;
    private readonly ILocalizationService _loc = localizationService;
    private readonly ILogger<DashboardCashflowPreviewCoordinator>? _logger = logger;

    public async Task<DashboardCashflowPreviewSnapshot> LoadAsync(string? accountId)
    {
        try
        {
            var data = await _loadCashflowOutlookUseCase.ExecuteAsync(accountId: accountId);
            var income = data.ProjectedIncome;
            var expenses = data.ProjectedExpenses;
            var notable = data.Days.Count(d => d.IsNotable);
            var hasPreview = income != 0 || expenses != 0 || notable > 0;
            var summary = hasPreview
                ? string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    _loc.GetString(ResourceKeys.Fmt_CashflowSummary),
                    income.ToString("C", CurrencyCulture.Instance),
                    expenses.ToString("C", CurrencyCulture.Instance))
                : string.Empty;

            return new DashboardCashflowPreviewSnapshot(
                income - expenses,
                income,
                expenses,
                notable,
                summary);
        }
        catch (FeatureGateException)
        {
            return Empty();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DashboardCashflowPreviewCoordinator: LoadAsync failed");
            return Empty();
        }
    }

    private static DashboardCashflowPreviewSnapshot Empty()
        => new(0, 0, 0, 0, string.Empty);
}
