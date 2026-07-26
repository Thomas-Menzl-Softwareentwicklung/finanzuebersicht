using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Dashboard;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Accessibility;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;
public partial class DashboardViewModel : MonthNavigationViewModel, ILocalizableViewModel, ICurrencyRefreshViewModel
{
    private readonly LoadDashboardMonthUseCase _loadDashboardMonthUseCase;
    private readonly LoadDashboardYearUseCase _loadDashboardYearUseCase;
    private readonly LoadForecastUseCase _loadForecastUseCase;
    private readonly GetDueRecurringWithHintsUseCase _getDueRecurringUseCase;
    private readonly BookDueRecurringInstanceUseCase _bookDueRecurringUseCase;
    private readonly SkipDueRecurringInstanceUseCase _skipDueRecurringUseCase;
    private readonly LoadCashflowOutlookUseCase _loadCashflowOutlookUseCase;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly GetDefaultBudgetTotalUseCase _getDefaultBudgetTotalUseCase;
    private readonly LoadActiveAccountsUseCase _loadActiveAccountsUseCase;
    private readonly GetEarliestTransactionYearUseCase _getEarliestTransactionYearUseCase;
    private readonly GetAccountBalancesUseCase _getAccountBalancesUseCase;
    private readonly ILocalizationService _loc;
    private readonly INavigationService _navigationService;
    private readonly IClock _clock;
    private readonly ILogger<DashboardViewModel>? _logger;

    // --- Monatsansicht ---

    [ObservableProperty]
    private decimal gesamtEinnahmen;

    [ObservableProperty]
    private decimal gesamtAusgaben;

    [ObservableProperty]
    private decimal bilanz;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMonthData))]
    private ObservableCollection<CategorySummary> kategorieAusgaben = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMonthData))]
    private ObservableCollection<CategorySummary> kategorieEinnahmen = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMonthData))]
    [NotifyPropertyChangedFor(nameof(HasBudgetHinweise))]
    private ObservableCollection<BudgetHintSummary> budgetHinweise = [];

    [ObservableProperty]
    private decimal budgetGesamt;

    [ObservableProperty]
    private decimal budgetVerbraucht;

    [ObservableProperty]
    private decimal budgetRest;

    [ObservableProperty]
    private decimal budgetTagesbudget;

    [ObservableProperty]
    private bool showBudgetTagesbudget;

    public bool HasBudgetHinweise => BudgetHinweise.Count > 0;

    [ObservableProperty]
    private bool istPrognose;

    [ObservableProperty]
    private decimal forecastTotal;

    [ObservableProperty]
    private bool hasForecast;

    // Trend Vormonatsvergleich
    [ObservableProperty]
    private decimal? trendProzent;

    [ObservableProperty]
    private string trendText = string.Empty;

    [ObservableProperty]
    private bool trendPositiv;

    // Für BarChart: Forecast-Balken und Budget-Linie
    [ObservableProperty]
    private int forecastBarMonth;

    [ObservableProperty]
    private decimal forecastBarValue;

    [ObservableProperty]
    private decimal jahrBudgetTotal;

    // --- Jahresansicht ---

    [ObservableProperty]
    private string jahrAnzeige = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasYearData))]
    private decimal jahrGesamtAusgaben;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasYearData))]
    private ObservableCollection<CategorySummary> jahrKategorien = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasYearData))]
    private List<MonthSummary> jahrMonate = [];

    [ObservableProperty]
    private string monthDonutAccessibilitySummary = string.Empty;

    [ObservableProperty]
    private string yearBarAccessibilitySummary = string.Empty;

    [ObservableProperty]
    private string yearDonutAccessibilitySummary = string.Empty;

    // --- Allgemein ---

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<KategorieFilterItem> availableKonten = [];

    [ObservableProperty]
    private KategorieFilterItem? selectedKontoFilterItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedAccountSaldo))]
    private string? selectedAccountId;

    [ObservableProperty]
    private decimal selectedAccountSaldo;

    public bool HasSelectedAccountSaldo => !string.IsNullOrWhiteSpace(SelectedAccountId);

    [ObservableProperty]
    private decimal gesamtSaldo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasKontenUebersicht))]
    private ObservableCollection<AccountOverviewItem> kontenUebersicht = [];

    public bool HasKontenUebersicht => KontenUebersicht.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsYearView))]
    [NotifyPropertyChangedFor(nameof(ShowMonthView))]
    [NotifyPropertyChangedFor(nameof(ShowYearView))]
    [NotifyPropertyChangedFor(nameof(ShowHeroSaldo))]
    [NotifyPropertyChangedFor(nameof(ShowSummaryBilanz))]
    private bool isMonthView = true;

    public bool IsYearView => !IsMonthView;

    public bool HasMonthData => KategorieAusgaben.Count > 0 || KategorieEinnahmen.Count > 0 || BudgetHinweise.Count > 0;

    public bool HasYearData => JahrMonate.Count > 0 || JahrKategorien.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAnyData))]
    [NotifyPropertyChangedFor(nameof(ShowMonthView))]
    [NotifyPropertyChangedFor(nameof(ShowYearView))]
    private bool hasAnyDataLoaded;

    public bool HasAnyData => HasAnyDataLoaded;
    public bool ShowMonthView => HasAnyDataLoaded && IsMonthView;
    public bool ShowYearView => HasAnyDataLoaded && IsYearView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDueItems))]
    [NotifyPropertyChangedFor(nameof(DueRecurringText))]
    private int dueRecurringCount;

    [ObservableProperty]
    private ObservableCollection<DueRecurringItem> dueRecurringItems = [];

    public bool HasDueItems => DueRecurringCount > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCashflowPreview))]
    private decimal cashflowNetAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCashflowPreview))]
    private decimal cashflowProjectedIncome;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCashflowPreview))]
    private decimal cashflowProjectedExpenses;

    [ObservableProperty]
    private string cashflowSummaryText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCashflowPreview))]
    private int cashflowNotableDays;

    public bool HasCashflowPreview => CashflowProjectedIncome > 0 || CashflowProjectedExpenses > 0 || CashflowNotableDays > 0;

    public bool ShowCashflowCompact => HasCashflowPreview && !IsCashflowSectionExpanded;

    public bool ShowCashflowExpanded => IsCashflowSectionExpanded && HasCashflowPreview;

    public bool ShowCashflowEmptyLink => IsCashflowSectionExpanded && !HasCashflowPreview;

    public bool ShowSecondarySections => HasAnyDataLoaded || HasKontenUebersicht;

    public bool ShowDueDetailsList => HasDueItems && IsDueDetailsExpanded;

    [ObservableProperty]
    private bool isBudgetSectionExpanded;

    [ObservableProperty]
    private bool isYearMonthTrendExpanded;

    [ObservableProperty]
    private bool isYearCategoriesExpanded;

    [ObservableProperty]
    private bool isMonthExpensesSectionExpanded;

    [ObservableProperty]
    private bool isMonthIncomeSectionExpanded;

    [ObservableProperty]
    private bool isDueDetailsExpanded;

    [ObservableProperty]
    private bool isAccountsSectionExpanded;

    [ObservableProperty]
    private bool isCashflowSectionExpanded;

    [ObservableProperty]
    private bool isFilterSectionExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowKontenInsightRow))]
    [NotifyPropertyChangedFor(nameof(ShowBudgetInsightRow))]
    [NotifyPropertyChangedFor(nameof(ShowCashflowInsightRow))]
    private bool showInsightRows;

    [ObservableProperty]
    private string kontenCompactSummary = string.Empty;

    [ObservableProperty]
    private string budgetInsightDetail = string.Empty;

    [ObservableProperty]
    private string dueRecurringCompactDetail = string.Empty;

    public bool ShowHeroSaldo => HasKontenUebersicht || HasSelectedAccountSaldo;

    public bool ShowKontenInsightRow => ShowInsightRows && HasKontenUebersicht;

    public bool ShowBudgetInsightRow => ShowInsightRows && GetBudgetWarnings().Any();

    public bool ShowCashflowInsightRow => ShowInsightRows && HasCashflowPreview;

    public string CashflowInsightDetail => HasCashflowPreview
        ? CashflowNetAmount.ToString("C", CurrencyCulture.Instance)
        : string.Empty;

    public decimal SummarySaldo => HasSelectedAccountSaldo ? SelectedAccountSaldo : GesamtSaldo;

    public string SummarySaldoLabel => HasSelectedAccountSaldo
        ? _loc.GetString(ResourceKeys.Lbl_Kontosaldo)
        : _loc.GetString(ResourceKeys.Lbl_Gesamtsaldo);

    public bool ShowSummaryBilanz => IsMonthView && HasMonthData;

    public bool ShowMonthKpis => IsMonthView && HasMonthData;

    public string BudgetSectionChevron => IsBudgetSectionExpanded ? "▼" : "▶";
    public string YearMonthTrendChevron => IsYearMonthTrendExpanded ? "▼" : "▶";
    public string YearCategoriesChevron => IsYearCategoriesExpanded ? "▼" : "▶";
    public string MonthExpensesSectionChevron => IsMonthExpensesSectionExpanded ? "▼" : "▶";
    public string MonthIncomeSectionChevron => IsMonthIncomeSectionExpanded ? "▼" : "▶";
    public string DueDetailsChevron => IsDueDetailsExpanded ? "▼" : "▶";
    public string AccountsSectionChevron => IsAccountsSectionExpanded ? "▼" : "▶";
    public string CashflowSectionChevron => IsCashflowSectionExpanded ? "▼" : "▶";
    public string FilterSectionChevron => IsFilterSectionExpanded ? "▼" : "▶";

    public string DueRecurringText => DueRecurringCount == 1
        ? _loc.GetString(ResourceKeys.Lbl_DauerauftraegeFaellig_Singular, DueRecurringCount)
        : _loc.GetString(ResourceKeys.Lbl_DauerauftraegeFaellig, DueRecurringCount);

    private int _aktuellesJahr;
    private int _minJahr;
    private bool _minJahrLoaded;
    private bool _foundTransactions;

    partial void OnSelectedKontoFilterItemChanged(KategorieFilterItem? value)
    {
        SelectedAccountId = value?.Id;
    }

    partial void OnSelectedAccountIdChanged(string? value)
    {
        _ = UpdateSelectedAccountSaldoAsync();
        _ = LoadDashboard();
    }

    partial void OnIsBudgetSectionExpandedChanged(bool value) => OnPropertyChanged(nameof(BudgetSectionChevron));

    partial void OnIsYearMonthTrendExpandedChanged(bool value) => OnPropertyChanged(nameof(YearMonthTrendChevron));

    partial void OnIsYearCategoriesExpandedChanged(bool value) => OnPropertyChanged(nameof(YearCategoriesChevron));

    partial void OnIsMonthExpensesSectionExpandedChanged(bool value) => OnPropertyChanged(nameof(MonthExpensesSectionChevron));

    partial void OnIsMonthIncomeSectionExpandedChanged(bool value) => OnPropertyChanged(nameof(MonthIncomeSectionChevron));

    partial void OnIsDueDetailsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(DueDetailsChevron));
        OnPropertyChanged(nameof(ShowDueDetailsList));
    }

    partial void OnIsAccountsSectionExpandedChanged(bool value) => OnPropertyChanged(nameof(AccountsSectionChevron));

    partial void OnIsCashflowSectionExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(CashflowSectionChevron));
        OnPropertyChanged(nameof(ShowCashflowCompact));
        OnPropertyChanged(nameof(ShowCashflowExpanded));
        OnPropertyChanged(nameof(ShowCashflowEmptyLink));
    }

    partial void OnIsFilterSectionExpandedChanged(bool value) => OnPropertyChanged(nameof(FilterSectionChevron));

    partial void OnShowInsightRowsChanged(bool value)
    {
        _settingsService.Set(SettingsKeys.DashboardInsightRowsEnabled, value.ToString().ToLowerInvariant());
    }

    partial void OnGesamtSaldoChanged(decimal value)
    {
        OnPropertyChanged(nameof(SummarySaldo));
        OnPropertyChanged(nameof(ShowHeroSaldo));
    }

    partial void OnSelectedAccountSaldoChanged(decimal value)
    {
        OnPropertyChanged(nameof(SummarySaldo));
        OnPropertyChanged(nameof(ShowHeroSaldo));
    }

    partial void OnBilanzChanged(decimal value)
    {
        OnPropertyChanged(nameof(ShowSummaryBilanz));
        OnPropertyChanged(nameof(ShowMonthKpis));
    }

    public DashboardViewModel(
        LoadDashboardMonthUseCase loadDashboardMonthUseCase,
        LoadDashboardYearUseCase loadDashboardYearUseCase,
        LoadForecastUseCase loadForecastUseCase,
        LoadCashflowOutlookUseCase loadCashflowOutlookUseCase,
        GetDueRecurringWithHintsUseCase getDueRecurringUseCase,
        BookDueRecurringInstanceUseCase bookDueRecurringUseCase,
        SkipDueRecurringInstanceUseCase skipDueRecurringUseCase,
        GetDefaultBudgetTotalUseCase getDefaultBudgetTotalUseCase,
        LoadActiveAccountsUseCase loadActiveAccountsUseCase,
        GetEarliestTransactionYearUseCase getEarliestTransactionYearUseCase,
        ILocalizationService localizationService,
        INavigationService navigationService,
        IDialogService dialogService,
        GetAccountBalancesUseCase getAccountBalancesUseCase,
        ISettingsService settingsService,
        IClock? clock = null,
        ILogger<DashboardViewModel>? logger = null) : base(clock)
    {
        _clock = clock ?? SystemClock.Instance;
        _aktuellesJahr = _clock.Today.Year;
        _minJahr = _clock.Today.Year - 10;
        _loadDashboardMonthUseCase = loadDashboardMonthUseCase;
        _loadDashboardYearUseCase = loadDashboardYearUseCase;
        _loadForecastUseCase = loadForecastUseCase;
        _loadCashflowOutlookUseCase = loadCashflowOutlookUseCase;
        _getDueRecurringUseCase = getDueRecurringUseCase;
        _bookDueRecurringUseCase = bookDueRecurringUseCase;
        _skipDueRecurringUseCase = skipDueRecurringUseCase;
        _getDefaultBudgetTotalUseCase = getDefaultBudgetTotalUseCase;
        _loadActiveAccountsUseCase = loadActiveAccountsUseCase;
        _getEarliestTransactionYearUseCase = getEarliestTransactionYearUseCase;
        _loc = localizationService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _getAccountBalancesUseCase = getAccountBalancesUseCase;
        _settingsService = settingsService;
        _logger = logger;
        IsBudgetSectionExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardBudgetExpanded);
        IsYearMonthTrendExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardYearMonthTrendExpanded);
        IsYearCategoriesExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardYearCategoriesExpanded, defaultExpanded: true);
        IsMonthExpensesSectionExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardMonthExpensesExpanded, defaultExpanded: true);
        IsMonthIncomeSectionExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardMonthIncomeExpanded);
        IsDueDetailsExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardDueDetailsExpanded);
        IsAccountsSectionExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardAccountsExpanded);
        IsCashflowSectionExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardCashflowExpanded);
        IsFilterSectionExpanded = ReadExpandedSetting(settingsService, SettingsKeys.DashboardFilterExpanded);
        ShowInsightRows = ReadExpandedSetting(settingsService, SettingsKeys.DashboardInsightRowsEnabled);
        UpdateJahrAnzeige();
    }

    private static bool ReadExpandedSetting(ISettingsService settingsService, string key, bool defaultExpanded = false)
    {
        var defaultValue = defaultExpanded.ToString().ToLowerInvariant();
        return bool.TryParse(settingsService.Get(key, defaultValue), out var expanded)
            ? expanded
            : defaultExpanded;
    }

    protected override async Task OnMonthChangedAsync() => await LoadDashboard();

    [RelayCommand]
    private async Task LoadDashboard()
    {
        CurrencyRefreshRegistry.Register(this);
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            await EnsureMinJahrLoadedAsync();
            await EnsureAccountFilterLoadedAsync();
            await UpdateKontenUebersichtAsync();
            await UpdateSelectedAccountSaldoAsync();
            if (IsMonthView)
            {
                await LadeMonatAsync();
            }
            else
            {
                await LadeJahrAsync();
            }
            await LadeFaelligeDauerauftraegeAsync();
            await LoadCashflowPreviewAsync();
            HasAnyDataLoaded = HasMonthData || HasYearData;
            OnPropertyChanged(nameof(ShowSecondarySections));
        }
        finally
        {
            IsLoading = false;
            UpdateInsightSummaries();
            OnPropertyChanged(nameof(ShowSummaryBilanz));
            OnPropertyChanged(nameof(ShowMonthKpis));
            OnPropertyChanged(nameof(SummarySaldoLabel));
        }
    }

    private async Task EnsureMinJahrLoadedAsync()
    {
        if (_minJahrLoaded && _foundTransactions) return;
        _minJahrLoaded = true;
        try
        {
            var earliestYear = await _getEarliestTransactionYearUseCase.ExecuteAsync();
            if (earliestYear.HasValue)
            {
                _minJahr = earliestYear.Value;
                _foundTransactions = true;
            }
            else
            {
                _foundTransactions = false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DashboardViewModel: EnsureMinJahrLoadedAsync failed");
        }
        PreviousYearCommand.NotifyCanExecuteChanged();
    }

    private async Task UpdateSelectedAccountSaldoAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedAccountId))
        {
            SelectedAccountSaldo = 0;
            return;
        }

        var balances = await _getAccountBalancesUseCase.ExecuteAsync();
        SelectedAccountSaldo = balances.FirstOrDefault(b => b.AccountId == SelectedAccountId)?.Saldo ?? 0;
    }

    private async Task UpdateKontenUebersichtAsync()
    {
        var balances = await _getAccountBalancesUseCase.ExecuteAsync();
        var active = balances.Where(b => !b.IsArchived).ToList();
        GesamtSaldo = active.Sum(b => b.Saldo);

        var maxAbs = active.Count > 0 ? active.Max(b => Math.Abs(b.Saldo)) : 0m;
        KontenUebersicht = new ObservableCollection<AccountOverviewItem>(
            active
                .OrderByDescending(b => Math.Abs(b.Saldo))
                .Select(b => new AccountOverviewItem
                {
                    AccountId = b.AccountId,
                    Name = b.AccountName,
                    Saldo = b.Saldo,
                    AnteilProzent = maxAbs > 0 ? Math.Abs(b.Saldo) / maxAbs * 100 : 0
                }));
    }

    private IEnumerable<BudgetHintSummary> GetBudgetWarnings() =>
        BudgetHinweise.Where(b => b.IstWarnung || b.IstAusgeschoepft);

    private void UpdateInsightSummaries()
    {
        var culture = CurrencyCulture.Instance;
        KontenCompactSummary = string.Join(" · ",
            KontenUebersicht.Take(3).Select(k => $"{k.Name} {k.Saldo.ToString("C", culture)}"));

        var budgetWarnings = GetBudgetWarnings().ToList();
        BudgetInsightDetail = budgetWarnings.Count switch
        {
            0 => string.Empty,
            1 => budgetWarnings[0].CategoryName,
            _ => _loc.GetString(ResourceKeys.Fmt_BudgetUeberLimitCount, budgetWarnings.Count)
        };

        DueRecurringCompactDetail = DueRecurringItems.FirstOrDefault()?.Recurring.Titel ?? string.Empty;

        OnPropertyChanged(nameof(ShowHeroSaldo));
        OnPropertyChanged(nameof(ShowKontenInsightRow));
        OnPropertyChanged(nameof(ShowBudgetInsightRow));
        OnPropertyChanged(nameof(ShowCashflowInsightRow));
        OnPropertyChanged(nameof(CashflowInsightDetail));
    }

    [RelayCommand]
    private Task SelectKontoFromOverview(AccountOverviewItem item)
    {
        SelectedKontoFilterItem = AvailableKonten.FirstOrDefault(k => k.Id == item.AccountId)
            ?? AvailableKonten.FirstOrDefault();
        return Task.CompletedTask;
    }

    private async Task EnsureAccountFilterLoadedAsync()
    {
        if (AvailableKonten.Count > 0) return;

        var accounts = await _loadActiveAccountsUseCase.ExecuteAsync();
        var items = new ObservableCollection<KategorieFilterItem>
        {
            new(null, _loc.GetString(ResourceKeys.Lbl_AlleKonten), ResourceKeys.Lbl_AlleKonten)
        };

        foreach (var account in accounts)
            items.Add(new KategorieFilterItem(account.Id, account.Name));

        AvailableKonten = items;
        SelectedKontoFilterItem = items[0];
    }

    private async Task LadeMonatAsync()
    {
        var data = await _loadDashboardMonthUseCase.ExecuteAsync(AktuellerMonat, _clock.Today, SelectedAccountId);

        IstPrognose = data.IstPrognose;
        GesamtEinnahmen = data.GesamtEinnahmen;
        GesamtAusgaben = data.GesamtAusgaben;
        Bilanz = data.Bilanz;
        KategorieAusgaben = new ObservableCollection<CategorySummary>(data.KategorieAusgaben);
        KategorieEinnahmen = new ObservableCollection<CategorySummary>(data.KategorieEinnahmen);
        BudgetHinweise = new ObservableCollection<BudgetHintSummary>(data.BudgetHinweise);
        BudgetGesamt = data.BudgetHinweise.Sum(b => b.BudgetBetrag);
        BudgetVerbraucht = data.BudgetHinweise.Sum(b => b.Verbrauch);
        BudgetRest = data.BudgetHinweise.Sum(b => b.Restbudget);
        ShowBudgetTagesbudget = data.BudgetHinweise.Any(b => b.ZeigeTagesbudget);
        var remainingDays = data.BudgetHinweise.FirstOrDefault(b => b.IstAktuellerMonat)?.VerbleibendeTage ?? 0;
        BudgetTagesbudget = remainingDays > 0
            ? data.BudgetHinweise.Sum(b => b.RestbudgetPositiv) / remainingDays
            : 0;

        // Vormonatsvergleich: nur Gesamt-Ausgaben via ReportingService (keine Kategorien nötig)
        var prevMonth = AktuellerMonat.AddMonths(-1);
        var prevData = await _loadDashboardMonthUseCase.ExecuteAsync(prevMonth, _clock.Today, SelectedAccountId);
        if (prevData.GesamtAusgaben > 0)
        {
            var pct = (GesamtAusgaben - prevData.GesamtAusgaben) / prevData.GesamtAusgaben * 100;
            TrendProzent = pct;
            TrendPositiv = pct < 0; // weniger Ausgaben = positiv (grün)
            TrendText = pct >= 0
                ? $"↑ +{pct:F0} %"
                : $"↓ {pct:F0} %";
        }
        else
        {
            TrendProzent = null;
            TrendPositiv = false;
            TrendText = string.Empty;
        }

        // Load forecast for current month only (not for past/future navigation)
        var today = _clock.Today;
        var isCurrentMonth = AktuellerMonat.Year == today.Year && AktuellerMonat.Month == today.Month;
        if (isCurrentMonth)
        {
            var nextMonth = AktuellerMonat.AddMonths(1);
            var forecast = await _loadForecastUseCase.ExecuteAsync(nextMonth.Year, nextMonth.Month, accountId: SelectedAccountId);
            ForecastTotal = forecast.ForecastedTotal;
            HasForecast = forecast.ForecastedTotal > 0;
        }
        else
        {
            HasForecast = false;
        }

        UpdateChartAccessibilitySummaries();
        OnPropertyChanged(nameof(ShowMonthKpis));
        OnPropertyChanged(nameof(ShowSummaryBilanz));
        OnPropertyChanged(nameof(ShowBudgetInsightRow));
    }

    private async Task LadeJahrAsync()
    {
        var data = await _loadDashboardYearUseCase.ExecuteAsync(_aktuellesJahr, SelectedAccountId);
        JahrGesamtAusgaben = data.GesamtAusgaben;
        JahrMonate = data.Monate;
        JahrKategorien = new ObservableCollection<CategorySummary>(data.Kategorien);

        // Summe der Default-Kategorie-Budgets (monat=null, jahr=null) als Referenzlinie
        // Hinweis: Monat- oder jahresspezifische Budget-Overrides werden bewusst nicht einberechnet,
        // da die Linie einen stabilen Jahres-Richtwert darstellen soll.
        JahrBudgetTotal = await _getDefaultBudgetTotalUseCase.ExecuteAsync();

        // Forecast-Balken für nächsten Monat (nur im aktuellen Jahr)
        var today = _clock.Today;
        if (_aktuellesJahr == today.Year)
        {
            var nextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            if (nextMonth.Year == _aktuellesJahr)
            {
                var forecast = await _loadForecastUseCase.ExecuteAsync(nextMonth.Year, nextMonth.Month, accountId: SelectedAccountId);
                ForecastBarMonth = nextMonth.Month;
                ForecastBarValue = forecast.ForecastedTotal;
            }
            else
            {
                ForecastBarMonth = 0;
                ForecastBarValue = 0;
            }
        }
        else
        {
            ForecastBarMonth = 0;
            ForecastBarValue = 0;
        }

        UpdateChartAccessibilitySummaries();
    }

    [RelayCommand]
    private async Task ZeigeMonate()
    {
        if (IsMonthView) return;
        IsMonthView = true;
        await LoadDashboard();
    }

    [RelayCommand]
    private async Task ZeigeJahr()
    {
        if (IsYearView) return;
        IsMonthView = false;
        await LoadDashboard();
    }

    [RelayCommand(CanExecute = nameof(CanGoNextYear))]
    private async Task NextYear()
    {
        _aktuellesJahr++;
        UpdateJahrAnzeige();
        await LoadDashboard();
    }

    [RelayCommand(CanExecute = nameof(CanGoPreviousYear))]
    private async Task PreviousYear()
    {
        _aktuellesJahr--;
        UpdateJahrAnzeige();
        await LoadDashboard();
    }

    private bool CanGoNextYear() => _aktuellesJahr < _clock.Today.Year + 1;

    private bool CanGoPreviousYear() => _aktuellesJahr > _minJahr;

    private void UpdateJahrAnzeige()
    {
        JahrAnzeige = _aktuellesJahr.ToString();
        NextYearCommand.NotifyCanExecuteChanged();
        PreviousYearCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadCashflowPreviewAsync()
    {
        try
        {
            var data = await _loadCashflowOutlookUseCase.ExecuteAsync(accountId: SelectedAccountId);
            CashflowNetAmount = data.ProjectedIncome - data.ProjectedExpenses;
            CashflowProjectedIncome = data.ProjectedIncome;
            CashflowProjectedExpenses = data.ProjectedExpenses;
            CashflowNotableDays = data.Days.Count(d => d.IsNotable);
            UpdateCashflowSummaryText();
        }
        catch (Finanzuebersicht.Core.Licensing.FeatureGateException)
        {
            // Free Store tier: Cashflow is Pro — hide preview without error noise
            CashflowNetAmount = 0;
            CashflowProjectedIncome = 0;
            CashflowProjectedExpenses = 0;
            CashflowNotableDays = 0;
            CashflowSummaryText = string.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DashboardViewModel: LoadCashflowPreviewAsync failed");
            CashflowNetAmount = 0;
            CashflowProjectedIncome = 0;
            CashflowProjectedExpenses = 0;
            CashflowNotableDays = 0;
            CashflowSummaryText = string.Empty;
        }

        OnPropertyChanged(nameof(ShowCashflowCompact));
        OnPropertyChanged(nameof(ShowCashflowExpanded));
        OnPropertyChanged(nameof(ShowCashflowEmptyLink));
        OnPropertyChanged(nameof(ShowCashflowInsightRow));
        OnPropertyChanged(nameof(CashflowInsightDetail));
    }

    [RelayCommand]
    private void ToggleBudgetSection()
    {
        IsBudgetSectionExpanded = !IsBudgetSectionExpanded;
        _settingsService.Set(SettingsKeys.DashboardBudgetExpanded, IsBudgetSectionExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleYearMonthTrend()
    {
        IsYearMonthTrendExpanded = !IsYearMonthTrendExpanded;
        _settingsService.Set(SettingsKeys.DashboardYearMonthTrendExpanded, IsYearMonthTrendExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleYearCategories()
    {
        IsYearCategoriesExpanded = !IsYearCategoriesExpanded;
        _settingsService.Set(SettingsKeys.DashboardYearCategoriesExpanded, IsYearCategoriesExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleMonthExpensesSection()
    {
        IsMonthExpensesSectionExpanded = !IsMonthExpensesSectionExpanded;
        _settingsService.Set(SettingsKeys.DashboardMonthExpensesExpanded, IsMonthExpensesSectionExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleMonthIncomeSection()
    {
        IsMonthIncomeSectionExpanded = !IsMonthIncomeSectionExpanded;
        _settingsService.Set(SettingsKeys.DashboardMonthIncomeExpanded, IsMonthIncomeSectionExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleDueDetails()
    {
        IsDueDetailsExpanded = !IsDueDetailsExpanded;
        _settingsService.Set(SettingsKeys.DashboardDueDetailsExpanded, IsDueDetailsExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleAccountsSection()
    {
        IsAccountsSectionExpanded = !IsAccountsSectionExpanded;
        _settingsService.Set(SettingsKeys.DashboardAccountsExpanded, IsAccountsSectionExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleCashflowSection()
    {
        IsCashflowSectionExpanded = !IsCashflowSectionExpanded;
        _settingsService.Set(SettingsKeys.DashboardCashflowExpanded, IsCashflowSectionExpanded.ToString().ToLowerInvariant());
    }

    [RelayCommand]
    private void ToggleFilterSection()
    {
        IsFilterSectionExpanded = !IsFilterSectionExpanded;
        _settingsService.Set(SettingsKeys.DashboardFilterExpanded, IsFilterSectionExpanded.ToString().ToLowerInvariant());
    }

    private async Task LadeFaelligeDauerauftraegeAsync()
    {
        const int dashboardPreviewDays = 7;
        var items = await _getDueRecurringUseCase.ExecuteAsync(_clock.Today);
        var actionable = items
            .Select(item =>
            {
                var hint = BuildDueRecurringHint(item, dashboardPreviewDays);
                if (hint is null)
                    return null;

                item.Hint = hint;
                return item;
            })
            .Where(item => item is not null)
            .Cast<DueRecurringItem>()
            .ToList();

        DueRecurringCount = actionable.Count;
        DueRecurringItems = new ObservableCollection<DueRecurringItem>(actionable);
        OnPropertyChanged(nameof(DueRecurringText));
        OnPropertyChanged(nameof(ShowDueDetailsList));
    }

    private string? BuildDueRecurringHint(DueRecurringItem item, int dashboardPreviewDays)
    {
        var daysUntil = (item.DueDate.Date - _clock.Today.Date).Days;
        return daysUntil switch
        {
            0 => _loc.GetString(ResourceKeys.Hint_HeuteFaellig),
            < 0 => _loc.GetString(ResourceKeys.Hint_UeberfaelligSeitTagen, -daysUntil),
            > 0 when daysUntil <= dashboardPreviewDays => _loc.GetString(ResourceKeys.Hint_FaelligInTagen, daysUntil),
            > 0 when item.Recurring.ReminderDaysBefore > 0 && daysUntil <= item.Recurring.ReminderDaysBefore
                => _loc.GetString(ResourceKeys.Hint_FaelligInTagen, daysUntil),
            _ => null
        };
    }

    [RelayCommand]
    private async Task BookDueRecurring(DueRecurringItem? item)
    {
        if (item == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_DauerauftragBuchen),
            _loc.GetString(ResourceKeys.Dlg_DauerauftragBuchenFrage, item.Recurring.Titel, item.DueDate.ToString("d")),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            await _bookDueRecurringUseCase.ExecuteAsync(item.Recurring.Id, item.InstanceDate);
            await LoadDashboard();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BookDueRecurring failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task SkipDueRecurring(DueRecurringItem? item)
    {
        if (item == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_DauerauftragUeberspringen),
            _loc.GetString(ResourceKeys.Dlg_DauerauftragUeberspringenFrage, item.Recurring.Titel),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            await _skipDueRecurringUseCase.ExecuteAsync(item.Recurring.Id, item.InstanceDate);
            await LoadDashboard();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SkipDueRecurring failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task ShiftDueRecurring(DueRecurringItem? item)
    {
        if (item == null) return;

        await _navigationService.GoToAsync(Routes.RecurringInstanceShift, new Dictionary<string, object>
        {
            [NavigationQueryKeys.RecurringId] = item.Recurring.Id,
            [NavigationQueryKeys.InstanceDate] = item.InstanceDate
        });
    }

    [RelayCommand]
    private async Task NavigateToDauerauftraege()
    {
        await _navigationService.GoToAsync("//RecurringTransactionsPage");
    }

    [RelayCommand]
    private async Task NavigateToTransaktionen()
    {
        await _navigationService.GoToAsync("//TransactionsPage");
    }

    [RelayCommand]
    private async Task NavigateToCashflow()
    {
        await _navigationService.GoToAsync(Routes.Cashflow);
    }

    public void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(SummarySaldoLabel));
        OnPropertyChanged(nameof(DueRecurringText));
        UpdateInsightSummaries();
        UpdateChartAccessibilitySummaries();
    }

    public void RefreshCurrencyDisplay()
    {
        OnPropertyChanged(nameof(GesamtEinnahmen));
        OnPropertyChanged(nameof(GesamtAusgaben));
        OnPropertyChanged(nameof(Bilanz));
        OnPropertyChanged(nameof(BudgetGesamt));
        OnPropertyChanged(nameof(BudgetVerbraucht));
        OnPropertyChanged(nameof(BudgetRest));
        OnPropertyChanged(nameof(BudgetTagesbudget));
        OnPropertyChanged(nameof(ForecastTotal));
        OnPropertyChanged(nameof(ForecastBarValue));
        OnPropertyChanged(nameof(JahrBudgetTotal));
        OnPropertyChanged(nameof(JahrGesamtAusgaben));
        OnPropertyChanged(nameof(SelectedAccountSaldo));
        OnPropertyChanged(nameof(GesamtSaldo));
        OnPropertyChanged(nameof(SummarySaldo));
        OnPropertyChanged(nameof(CashflowNetAmount));
        OnPropertyChanged(nameof(CashflowProjectedIncome));
        OnPropertyChanged(nameof(CashflowProjectedExpenses));
        UpdateCashflowSummaryText();
        OnPropertyChanged(nameof(TrendProzent));

        if (KategorieAusgaben.Count > 0)
            KategorieAusgaben = new ObservableCollection<CategorySummary>(KategorieAusgaben);
        if (KategorieEinnahmen.Count > 0)
            KategorieEinnahmen = new ObservableCollection<CategorySummary>(KategorieEinnahmen);
        CurrencyDisplayRefresh.Rebind(BudgetHinweise);
        if (JahrKategorien.Count > 0)
            JahrKategorien = CurrencyDisplayRefresh.Clone(JahrKategorien);
        if (KontenUebersicht.Count > 0)
            KontenUebersicht = CurrencyDisplayRefresh.Clone(KontenUebersicht);
        if (DueRecurringItems.Count > 0)
            DueRecurringItems = CurrencyDisplayRefresh.Clone(DueRecurringItems);

        if (JahrMonate.Count > 0)
            JahrMonate = [.. JahrMonate];

        UpdateInsightSummaries();
        UpdateChartAccessibilitySummaries();
    }

    private void UpdateCashflowSummaryText()
    {
        CashflowSummaryText = HasCashflowPreview
            ? string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                _loc.GetString(ResourceKeys.Fmt_CashflowSummary),
                CashflowProjectedIncome.ToString("C", CurrencyCulture.Instance),
                CashflowProjectedExpenses.ToString("C", CurrencyCulture.Instance))
            : string.Empty;
    }

    private void UpdateChartAccessibilitySummaries()
    {
        var culture = CurrencyCulture.Instance;
        MonthDonutAccessibilitySummary = ChartAccessibilitySummaryBuilder.BuildCategoryDonutSummary(KategorieAusgaben, _loc, culture);
        YearDonutAccessibilitySummary = ChartAccessibilitySummaryBuilder.BuildCategoryDonutSummary(JahrKategorien, _loc, culture);
        YearBarAccessibilitySummary = ChartAccessibilitySummaryBuilder.BuildMonthBarSummary(
            JahrMonate, _loc, culture, ForecastBarMonth, ForecastBarValue);
    }
}
