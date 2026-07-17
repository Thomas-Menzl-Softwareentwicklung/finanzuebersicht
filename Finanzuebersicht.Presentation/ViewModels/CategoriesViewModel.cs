using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class CategoriesViewModel(
    DeleteCategoryUseCase deleteCategoryUseCase,
    LoadCategoriesUseCase loadCategoriesUseCase,
    LoadAccountsUseCase loadAccountsUseCase,
    GetAccountBalancesUseCase getAccountBalancesUseCase,
    SaveAccountDetailUseCase saveAccountDetailUseCase,
    ToggleAccountArchiveUseCase toggleAccountArchiveUseCase,
    DeleteAccountUseCase deleteAccountUseCase,
    CategoryDetailViewModel createCategoryViewModel,
    ICategoryCreateSheetService categoryCreateSheetService,
    ILocalizationService localizationService,
    INavigationService navigationService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<CategoriesViewModel>? logger = null) : ObservableObject, IAutoLoadViewModel, ILocalizableViewModel, ICurrencyRefreshViewModel
{
    private readonly DeleteCategoryUseCase _deleteCategoryUseCase = deleteCategoryUseCase;
    private readonly LoadCategoriesUseCase _loadCategoriesUseCase = loadCategoriesUseCase;
    private readonly LoadAccountsUseCase _loadAccountsUseCase = loadAccountsUseCase;
    private readonly GetAccountBalancesUseCase _getAccountBalancesUseCase = getAccountBalancesUseCase;
    private readonly SaveAccountDetailUseCase _saveAccountDetailUseCase = saveAccountDetailUseCase;
    private readonly ToggleAccountArchiveUseCase _toggleAccountArchiveUseCase = toggleAccountArchiveUseCase;
    private readonly DeleteAccountUseCase _deleteAccountUseCase = deleteAccountUseCase;
    private readonly CategoryDetailViewModel _createCategoryViewModel = createCategoryViewModel;
    private readonly ICategoryCreateSheetService _categoryCreateSheetService = categoryCreateSheetService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<CategoriesViewModel>? _logger = logger;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadKategorienCommand;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKategorienVisible))]
    [NotifyPropertyChangedFor(nameof(IsKontenVisible))]
    [NotifyPropertyChangedFor(nameof(IsKategorienEmpty))]
    private ObservableCollection<Category> kategorien = [];

    public bool IsKategorienEmpty => Kategorien.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGesamtSaldoHeader))]
    [NotifyPropertyChangedFor(nameof(IsKontenEmpty))]
    [NotifyPropertyChangedFor(nameof(IsKontenEmptyStateVisible))]
    private ObservableCollection<AccountListItem> konten = [];

    public bool IsKontenEmpty => Konten.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGesamtSaldoHeader))]
    private decimal gesamtSaldoAktiv;

    public bool ShowGesamtSaldoHeader => Konten.Any(k => !k.IsArchived);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKategorienVisible))]
    [NotifyPropertyChangedFor(nameof(IsKontenVisible))]
    private int selectedSectionIndex;

    public bool IsKategorienVisible => SelectedSectionIndex == 0;
    public bool IsKontenVisible => SelectedSectionIndex == 1;

    public string FabAccessibilityDescription => IsKontenVisible
        ? _loc.GetString(ResourceKeys.A11y_KontoHinzufuegen)
        : _loc.GetString(ResourceKeys.A11y_KategorieHinzufuegen);

    partial void OnSelectedSectionIndexChanged(int value)
    {
        OnPropertyChanged(nameof(FabAccessibilityDescription));
        if (value == 0)
            ShowAddKontoForm = false;
    }

    public void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(FabAccessibilityDescription));
        OnPropertyChanged(nameof(NeuesKontoFormTitle));
        OnPropertyChanged(nameof(VerfuegbareKontoTypen));
        _ = LoadKategorienCore();
    }

    public void RefreshCurrencyDisplay() => _ = LoadKategorienCore(force: true);

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKontenEmptyStateVisible))]
    private bool showAddKontoForm;

    [ObservableProperty]
    private string neuerKontoName = string.Empty;

    [ObservableProperty]
    private AccountTypeOption? selectedKontoTypeOption;

    [ObservableProperty]
    private string neuerAnfangssaldoText = string.Empty;

    public bool IsKontenEmptyStateVisible => IsKontenEmpty && !ShowAddKontoForm;

    public string NeuesKontoFormTitle => _loc.GetString(ResourceKeys.Title_KontoHinzufuegen);

    public List<AccountTypeOption> VerfuegbareKontoTypen =>
    [
        new(AccountType.Girokonto, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Girokonto))),
        new(AccountType.Tagesgeld, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Tagesgeld))),
        new(AccountType.Kreditkarte, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Kreditkarte))),
        new(AccountType.Bargeld, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Bargeld))),
        new(AccountType.Depot, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Depot))),
        new(AccountType.Sonstiges, _loc.GetString(EnumResourceKeys.GetAccountType(AccountType.Sonstiges)))
    ];

    [RelayCommand]
    private void ShowKategorien() => SelectedSectionIndex = 0;

    [RelayCommand]
    private void ShowKonten() => SelectedSectionIndex = 1;

    [RelayCommand]
    private Task LoadKategorien() => LoadKategorienCore();

    private async Task LoadKategorienCore(bool force = false)
    {
        CurrencyRefreshRegistry.Register(this);
        if (!force && IsLoading) return;
        IsLoading = true;

        try
        {
            var liste = await _loadCategoriesUseCase.ExecuteAsync();
            Kategorien = new ObservableCollection<Category>(liste);
            var accounts = await _loadAccountsUseCase.ExecuteAsync();
            var balances = await _getAccountBalancesUseCase.ExecuteAsync();
            var balanceById = balances.ToDictionary(b => b.AccountId);
            Konten = new ObservableCollection<AccountListItem>(
                accounts
                    .OrderBy(a => a.IsArchived)
                    .ThenBy(a => a.Name)
                    .Select(a =>
                    {
                        balanceById.TryGetValue(a.Id, out var summary);
                        return new AccountListItem(a, summary)
                        {
                            BalanceBreakdownText = summary is { OpeningBalance: not 0 }
                                ? _loc.GetString(
                                    ResourceKeys.Fmt_KontoSaldoAufschluesselung,
                                    summary.OpeningBalance.ToString("C", CurrencyCulture.Instance),
                                    summary.TransactionBalance.ToString("C", CurrencyCulture.Instance))
                                : null
                        };
                    }));
            GesamtSaldoAktiv = balances.Where(b => !b.IsArchived).Sum(b => b.Saldo);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoriesViewModel: {Context}", nameof(LoadKategorien));
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
    private async Task DeleteKategorie(Category kategorie)
    {
        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_KategorieLoeschen),
            _loc.GetString(ResourceKeys.Dlg_KategorieLoeschenFrage, kategorie.Name),
            _loc.GetString(ResourceKeys.Btn_Ja), _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            await _deleteCategoryUseCase.ExecuteAsync(kategorie.Id);
            Kategorien.Remove(kategorie);
            OnPropertyChanged(nameof(IsKategorienEmpty));
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoriesViewModel: {Context}", nameof(DeleteKategorie));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task DeleteKonto(AccountListItem konto)
    {
        if (!konto.CanDelete) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_KontoLoeschen),
            _loc.GetString(ResourceKeys.Dlg_KontoLoeschenFrage, konto.Name),
            _loc.GetString(ResourceKeys.Btn_Ja), _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            await _deleteAccountUseCase.ExecuteAsync(konto.Account.Id);
            Konten.Remove(konto);
            OnPropertyChanged(nameof(IsKontenEmpty));
            OnPropertyChanged(nameof(IsKontenEmptyStateVisible));
            OnPropertyChanged(nameof(ShowGesamtSaldoHeader));
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoriesViewModel: {Context}", nameof(DeleteKonto));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task ToggleKontoArchivierung(AccountListItem konto)
    {
        if (!konto.CanArchive) return;

        var setArchived = !konto.IsArchived;
        var confirmTitle = setArchived
            ? _loc.GetString(ResourceKeys.Dlg_KontoArchivieren)
            : _loc.GetString(ResourceKeys.Dlg_KontoReaktivieren);
        var confirmBody = setArchived
            ? _loc.GetString(ResourceKeys.Dlg_KontoArchivierenFrage, konto.Name)
            : _loc.GetString(ResourceKeys.Dlg_KontoReaktivierenFrage, konto.Name);
        var confirm = await _dialogService.ShowConfirmationAsync(
            confirmTitle,
            confirmBody,
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            await _toggleAccountArchiveUseCase.ExecuteAsync(konto.Account, setArchived);
            await LoadKategorien();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoriesViewModel: {Context}", nameof(ToggleKontoArchivierung));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private void ToggleAddKontoForm()
    {
        ShowAddKontoForm = !ShowAddKontoForm;
        if (!ShowAddKontoForm)
            return;

        NeuerKontoName = string.Empty;
        NeuerAnfangssaldoText = string.Empty;
        SelectedKontoTypeOption = VerfuegbareKontoTypen.FirstOrDefault(t => t.Value == AccountType.Girokonto)
            ?? VerfuegbareKontoTypen.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveNewKonto()
    {
        if (string.IsNullOrWhiteSpace(NeuerKontoName))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_TitelErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        if (!TryParseAnfangssaldo(out var openingBalance))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            var type = SelectedKontoTypeOption?.Value ?? AccountType.Girokonto;
            await _saveAccountDetailUseCase.ExecuteAsync(
                null, NeuerKontoName.Trim(), type, openingBalance: openingBalance);
            ShowAddKontoForm = false;
            _appEvents.NotifyDataChanged();
            await LoadKategorienCore(force: true);
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoriesViewModel: {Context}", nameof(SaveNewKonto));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task GoToDetail(object? item = null)
    {
        if (item is AccountListItem kontoItem)
        {
            var kontoParameter = new Dictionary<string, object>
            {
                ["Account"] = kontoItem.Account
            };
            await _navigationService.GoToAsync(Routes.AccountDetail, kontoParameter);
            return;
        }

        if (item == null && IsKontenVisible)
        {
            ToggleAddKontoForm();
            return;
        }

        if (item == null && IsKategorienVisible)
        {
            _createCategoryViewModel.ResetForCreate();
            if (await _categoryCreateSheetService.ShowAsync(_createCategoryViewModel))
                await LoadKategorienCore(force: true);
            return;
        }

        if (item is Category kategorie)
        {
            var parameter = new Dictionary<string, object> { ["Category"] = kategorie };
            await _navigationService.GoToAsync(Routes.CategoryDetail, parameter);
            return;
        }

        await _navigationService.GoToAsync(Routes.CategoryDetail);
    }

    private bool TryParseAnfangssaldo(out decimal openingBalance)
    {
        if (string.IsNullOrWhiteSpace(NeuerAnfangssaldoText))
        {
            openingBalance = 0m;
            return true;
        }

        return decimal.TryParse(
            NeuerAnfangssaldoText,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out openingBalance);
    }
}
