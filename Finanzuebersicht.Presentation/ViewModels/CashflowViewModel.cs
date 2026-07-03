using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Dashboard;
using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class CashflowViewModel(
    LoadCashflowOutlookUseCase loadCashflowOutlookUseCase,
    LoadActiveAccountsUseCase loadActiveAccountsUseCase,
    ILocalizationService localizationService,
    IDialogService dialogService,
    ILogger<CashflowViewModel>? logger = null) : ObservableObject, IAutoLoadViewModel, ICurrencyRefreshViewModel
{
    private readonly LoadCashflowOutlookUseCase _loadCashflowOutlookUseCase = loadCashflowOutlookUseCase;
    private readonly LoadActiveAccountsUseCase _loadActiveAccountsUseCase = loadActiveAccountsUseCase;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILogger<CashflowViewModel>? _logger = logger;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadCashflowCommand;

    [ObservableProperty]
    private ObservableCollection<CashflowDayGroup> days = [];

    [ObservableProperty]
    private ObservableCollection<KategorieFilterItem> availableKonten = [];

    [ObservableProperty]
    private KategorieFilterItem? selectedKontoFilterItem;

    [ObservableProperty]
    private string? selectedAccountId;

    [ObservableProperty]
    private decimal projectedIncome;

    [ObservableProperty]
    private decimal projectedExpenses;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDays))]
    private bool hasData;

    public bool HasDays => Days.Count > 0;

    partial void OnSelectedKontoFilterItemChanged(KategorieFilterItem? value)
    {
        SelectedAccountId = value?.Id;
        _ = LoadCashflow();
    }

    [RelayCommand]
    private async Task LoadCashflow()
    {
        CurrencyRefreshRegistry.Register(this);
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            await EnsureAccountFilterLoadedAsync();
            var data = await _loadCashflowOutlookUseCase.ExecuteAsync(accountId: SelectedAccountId);
            Days = new ObservableCollection<CashflowDayGroup>(data.Days);
            ProjectedIncome = data.ProjectedIncome;
            ProjectedExpenses = data.ProjectedExpenses;
            HasData = data.Days.Count > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CashflowViewModel load failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LadenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        finally
        {
            IsLoading = false;
        }
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

    public void RefreshCurrencyDisplay()
    {
        OnPropertyChanged(nameof(ProjectedIncome));
        OnPropertyChanged(nameof(ProjectedExpenses));
        if (Days.Count > 0)
            Days = CurrencyDisplayRefresh.Clone(Days);
    }
}
