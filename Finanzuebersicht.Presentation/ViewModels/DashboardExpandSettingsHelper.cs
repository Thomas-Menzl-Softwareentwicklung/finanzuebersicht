using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.ViewModels;

/// <summary>
/// Persists dashboard section expand/collapse flags in settings.
/// </summary>
public sealed class DashboardExpandSettingsHelper(ISettingsService settingsService)
{
    private readonly ISettingsService _settingsService = settingsService;

    public bool Read(string key, bool defaultValue = false)
        => bool.TryParse(_settingsService.Get(key, defaultValue.ToString().ToLowerInvariant()), out var value)
            ? value
            : defaultValue;

    public void Write(string key, bool value)
        => _settingsService.Set(key, value.ToString().ToLowerInvariant());

    public bool Toggle(string key, bool current)
    {
        var next = !current;
        Write(key, next);
        return next;
    }

    public static class Keys
    {
        public const string Budget = SettingsKeys.DashboardBudgetExpanded;
        public const string YearMonthTrend = SettingsKeys.DashboardYearMonthTrendExpanded;
        public const string YearCategories = SettingsKeys.DashboardYearCategoriesExpanded;
        public const string MonthExpenses = SettingsKeys.DashboardMonthExpensesExpanded;
        public const string MonthIncome = SettingsKeys.DashboardMonthIncomeExpanded;
        public const string DueDetails = SettingsKeys.DashboardDueDetailsExpanded;
        public const string Accounts = SettingsKeys.DashboardAccountsExpanded;
        public const string Cashflow = SettingsKeys.DashboardCashflowExpanded;
        public const string Filter = SettingsKeys.DashboardFilterExpanded;
    }
}
