using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class AccountDetailViewModel(
    SaveAccountDetailUseCase saveAccountDetailUseCase,
    GetAccountBalancesUseCase getAccountBalancesUseCase,
    ReconcileAccountBalanceUseCase reconcileAccountBalanceUseCase,
    INavigationService navigationService,
    ILocalizationService localizationService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<AccountDetailViewModel>? logger = null) : ObservableObject, IApplyQueryAttributes, ILocalizableViewModel
{
    private readonly SaveAccountDetailUseCase _saveAccountDetailUseCase = saveAccountDetailUseCase;
    private readonly GetAccountBalancesUseCase _getAccountBalancesUseCase = getAccountBalancesUseCase;
    private readonly ReconcileAccountBalanceUseCase _reconcileAccountBalanceUseCase = reconcileAccountBalanceUseCase;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<AccountDetailViewModel>? _logger = logger;
    private Account? _existingAccount;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private AccountType type = AccountType.Girokonto;

    [ObservableProperty]
    private bool isArchived;

    [ObservableProperty]
    private AccountTypeOption? selectedTypeOption;

    [ObservableProperty]
    private string openingBalanceText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOpeningBalanceDate))]
    private bool useOpeningBalanceDate;

    [ObservableProperty]
    private DateTime openingBalanceDate = DateTime.Today;

    [ObservableProperty]
    private string calculatedBalanceText = string.Empty;

    [ObservableProperty]
    private string actualBalanceText = string.Empty;

    public bool HasOpeningBalanceDate => UseOpeningBalanceDate;
    public bool CanReconcile => _existingAccount != null;

    public string PageTitle => _existingAccount == null
        ? _loc.GetString(ResourceKeys.Title_KontoHinzufuegen)
        : _loc.GetString(ResourceKeys.Title_KontoBearbeiten);

    public bool IsSystemAccount => _existingAccount?.IsSystemAccount == true;
    public bool CanArchive => !IsSystemAccount;
    public string ArchiveStatusText => IsArchived
        ? _loc.GetString(ResourceKeys.Lbl_Archiviert)
        : _loc.GetString(ResourceKeys.Lbl_Aktiv);
    public string SystemAccountHint => IsSystemAccount
        ? _loc.GetString(ResourceKeys.Hint_Systemkonto)
        : string.Empty;

    partial void OnIsArchivedChanged(bool value) => OnPropertyChanged(nameof(ArchiveStatusText));

    public List<AccountTypeOption> VerfuegbareTypen =>
    [
        new(AccountType.Girokonto, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Girokonto))),
        new(AccountType.Tagesgeld, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Tagesgeld))),
        new(AccountType.Kreditkarte, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Kreditkarte))),
        new(AccountType.Bargeld, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Bargeld))),
        new(AccountType.Depot, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Depot))),
        new(AccountType.Sonstiges, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Sonstiges)))
    ];

    public void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(VerfuegbareTypen));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ArchiveStatusText));
        OnPropertyChanged(nameof(SystemAccountHint));
        SelectedTypeOption = VerfuegbareTypen.FirstOrDefault(t => t.Value == Type);
        if (_existingAccount != null)
            _ = LoadCalculatedBalanceAsync();
    }

    public Account? Account
    {
        set
        {
            if (value == null) return;

            _existingAccount = value;
            Name = value.Name;
            Type = value.Type;
            IsArchived = value.IsArchived;
            OpeningBalanceText = value.OpeningBalance.ToString("F2", CultureInfo.CurrentCulture);
            UseOpeningBalanceDate = value.OpeningBalanceDate.HasValue;
            OpeningBalanceDate = value.OpeningBalanceDate ?? DateTime.Today;
            SelectedTypeOption = VerfuegbareTypen.FirstOrDefault(t => t.Value == value.Type);
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsSystemAccount));
            OnPropertyChanged(nameof(CanArchive));
            OnPropertyChanged(nameof(SystemAccountHint));
            OnPropertyChanged(nameof(ArchiveStatusText));
            OnPropertyChanged(nameof(CanReconcile));
            _ = LoadCalculatedBalanceAsync();
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationQueryKeys.Account, out var val) && val is Account a)
            Account = a;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        if (!TryParseOpeningBalance(out var openingBalance))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            Type = SelectedTypeOption?.Value ?? Type;
            var openingBalanceDate = UseOpeningBalanceDate ? OpeningBalanceDate : (DateTime?)null;
            await _saveAccountDetailUseCase.ExecuteAsync(
                _existingAccount, Name, Type, IsArchived, openingBalance, openingBalanceDate);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AccountDetailViewModel: Save failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task Reconcile()
    {
        if (_existingAccount == null) return;

        if (!decimal.TryParse(ActualBalanceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var actualBalance))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            var result = await _reconcileAccountBalanceUseCase.ExecuteAsync(_existingAccount.Id, actualBalance);
            OpeningBalanceText = result.NewOpeningBalance.ToString("F2", CultureInfo.CurrentCulture);
            _existingAccount.OpeningBalance = result.NewOpeningBalance;
            await LoadCalculatedBalanceAsync();
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_SaldoAbgeglichen));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AccountDetailViewModel: Reconcile failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    private async Task LoadCalculatedBalanceAsync()
    {
        if (_existingAccount == null)
        {
            CalculatedBalanceText = string.Empty;
            return;
        }

        var summaries = await _getAccountBalancesUseCase.ExecuteAsync();
        var summary = summaries.FirstOrDefault(s => s.AccountId == _existingAccount.Id);
        CalculatedBalanceText = summary?.Saldo.ToString("C", CurrencyCulture.Instance) ?? string.Empty;
    }

    private bool TryParseOpeningBalance(out decimal openingBalance)
    {
        if (string.IsNullOrWhiteSpace(OpeningBalanceText))
        {
            openingBalance = 0m;
            return true;
        }

        return decimal.TryParse(
            OpeningBalanceText,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out openingBalance);
    }
}

public sealed record AccountTypeOption(AccountType Value, string DisplayName);
