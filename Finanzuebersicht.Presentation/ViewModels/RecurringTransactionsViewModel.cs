using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class RecurringTransactionsViewModel(
    DeleteRecurringTransactionUseCase deleteRecurringTransactionUseCase,
    LoadRecurringTransactionsUseCase loadRecurringTransactionsUseCase,
    ToggleRecurringTransactionActiveUseCase toggleRecurringTransactionActiveUseCase,
    RecurringTransactionDetailViewModel createRecurringTransactionViewModel,
    IRecurringTransactionCreateSheetService recurringTransactionCreateSheetService,
    ILocalizationService localizationService,
    INavigationService navigationService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<RecurringTransactionsViewModel>? logger = null) : ObservableObject, IAutoLoadViewModel, ICurrencyRefreshViewModel
{
    private readonly DeleteRecurringTransactionUseCase _deleteRecurringTransactionUseCase = deleteRecurringTransactionUseCase;
    private readonly LoadRecurringTransactionsUseCase _loadRecurringTransactionsUseCase = loadRecurringTransactionsUseCase;
    private readonly ToggleRecurringTransactionActiveUseCase _toggleRecurringTransactionActiveUseCase = toggleRecurringTransactionActiveUseCase;
    private readonly RecurringTransactionDetailViewModel _createRecurringTransactionViewModel = createRecurringTransactionViewModel;
    private readonly IRecurringTransactionCreateSheetService _recurringTransactionCreateSheetService = recurringTransactionCreateSheetService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<RecurringTransactionsViewModel>? _logger = logger;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadDauerauftraegeCommand;

    [ObservableProperty]
    private ObservableCollection<RecurringTransaction> dauerauftraege = [];

    [ObservableProperty]
    private bool isLoading;

    [RelayCommand]
    private async Task LoadDauerauftraege()
    {
        CurrencyRefreshRegistry.Register(this);
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var liste = await _loadRecurringTransactionsUseCase.ExecuteAsync();
            Dauerauftraege = new ObservableCollection<RecurringTransaction>(liste);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RecurringTransactionsViewModel: {Context}", nameof(LoadDauerauftraege));
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

    [RelayCommand]
    private async Task ToggleAktiv(RecurringTransaction dauerauftrag)
    {
        await _toggleRecurringTransactionActiveUseCase.ExecuteAsync(dauerauftrag);
        await LoadDauerauftraege();
    }

    [RelayCommand]
    private async Task DeleteDauerauftrag(RecurringTransaction dauerauftrag)
    {
        var bestaetigt = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_DauerauftragLoeschen),
            _loc.GetString(ResourceKeys.Dlg_DauerauftragLoeschenFrage, dauerauftrag.Titel),
            _loc.GetString(ResourceKeys.Btn_Ja), _loc.GetString(ResourceKeys.Btn_Nein));

        if (!bestaetigt) return;

        try
        {
            await _deleteRecurringTransactionUseCase.ExecuteAsync(dauerauftrag.Id);
            Dauerauftraege.Remove(dauerauftrag);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RecurringTransactionsViewModel: {Context}", nameof(DeleteDauerauftrag));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task GoToDetail(RecurringTransaction? dauerauftrag)
    {
        if (dauerauftrag == null)
        {
            await _createRecurringTransactionViewModel.ResetForCreateAsync();
            if (await _recurringTransactionCreateSheetService.ShowAsync(_createRecurringTransactionViewModel))
                await LoadDauerauftraege();
            return;
        }

        var parameter = new Dictionary<string, object>
        {
            [NavigationQueryKeys.RecurringTransaction] = dauerauftrag
        };

        await _navigationService.GoToAsync(Routes.RecurringTransactionDetail, parameter);
    }

    public void RefreshCurrencyDisplay()
    {
        if (Dauerauftraege.Count > 0)
            Dauerauftraege = CurrencyDisplayRefresh.Clone(Dauerauftraege);
    }
}
