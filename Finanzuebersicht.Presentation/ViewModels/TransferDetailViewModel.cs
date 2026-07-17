using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class TransferDetailViewModel(
    SaveTransferUseCase saveTransferUseCase,
    LoadActiveAccountsUseCase loadActiveAccountsUseCase,
    INavigationService navigationService,
    IDialogService dialogService,
    ILocalizationService localizationService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<TransferDetailViewModel>? logger = null,
    Finanzuebersicht.Core.Services.IClock? clock = null) : ObservableObject, IAutoLoadViewModel
{
    private readonly SaveTransferUseCase _saveTransferUseCase = saveTransferUseCase;
    private readonly LoadActiveAccountsUseCase _loadActiveAccountsUseCase = loadActiveAccountsUseCase;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<TransferDetailViewModel>? _logger = logger;
    private readonly Finanzuebersicht.Core.Services.IClock _clock = clock ?? Finanzuebersicht.Core.Services.SystemClock.Instance;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadAccountsCommand;

    [ObservableProperty]
    private ObservableCollection<Account> accounts = [];

    [ObservableProperty]
    private Account? sourceAccount;

    [ObservableProperty]
    private Account? targetAccount;

    [ObservableProperty]
    private string amountText = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    private DateTime date;

    [RelayCommand]
    private async Task LoadAccounts()
    {
        Date = _clock.Today;
        if (string.IsNullOrWhiteSpace(Title))
            Title = _loc.GetString(ResourceKeys.Title_Umbuchung);

        var accounts = await _loadActiveAccountsUseCase.ExecuteAsync();
        Accounts = new ObservableCollection<Account>(accounts);

        SourceAccount = accounts.FirstOrDefault(a => a.SystemKey == Finanzuebersicht.Constants.SystemAccountKeys.Default)
            ?? accounts.FirstOrDefault();
        TargetAccount = accounts.FirstOrDefault(a => a.Id != SourceAccount?.Id);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!decimal.TryParse(AmountText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var amount) || amount <= 0)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_BetragGroesserNull),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        if (SourceAccount == null || TargetAccount == null || SourceAccount.Id == TargetAccount.Id)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_TransferKontenUnterschiedlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            var title = string.IsNullOrWhiteSpace(Title)
                ? _loc.GetString(ResourceKeys.Title_Umbuchung)
                : Title.Trim();

            await _saveTransferUseCase.ExecuteAsync(
                SourceAccount.Id,
                TargetAccount.Id,
                amount,
                Date,
                title,
                Note);

            _appEvents.NotifyDataChanged();
            await _navigationService.GoBackAsync();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving transfer");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }
}
