using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class TransactionsViewModel(
    DeleteTransactionUseCase deleteTransactionUseCase,
    RestoreTransactionUseCase restoreTransactionUseCase,
    LoadTransactionsMonthUseCase loadTransactionsMonthUseCase,
    SearchTransactionsUseCase searchTransactionsUseCase,
    INavigationService navigationService,
    ImportService importService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    ILocalizationService localizationService,
    LoadCategoriesUseCase loadCategoriesUseCase,
    LoadAccountsUseCase loadAccountsUseCase,
    IMainThreadDispatcher dispatcher,
    IFilePicker filePicker,
    IAppEvents appEvents,
    ILogger<TransactionsViewModel> logger,
    IImportSessionStore? importSessionStore = null,
    LoadTransactionTemplatesUseCase? loadTransactionTemplatesUseCase = null,
    DeleteTransactionTemplateUseCase? deleteTransactionTemplateUseCase = null,
    UseTransactionTemplateUseCase? useTransactionTemplateUseCase = null,
    Finanzuebersicht.Core.Licensing.ILicenseService? licenseService = null,
    QuickExpenseCaptureViewModel? quickExpenseCaptureViewModel = null,
    IQuickExpenseCaptureSheetService? quickExpenseCaptureSheetService = null,
    CountUncategorizedTransactionsUseCase? countUncategorizedTransactionsUseCase = null,
    IUncategorizedCategoryService? uncategorizedCategoryService = null) : MonthNavigationViewModel, IAutoLoadViewModel, ICurrencyRefreshViewModel
{
    private readonly DeleteTransactionUseCase _deleteTransactionUseCase = deleteTransactionUseCase;
    private readonly RestoreTransactionUseCase _restoreTransactionUseCase = restoreTransactionUseCase;
    private readonly LoadTransactionsMonthUseCase _loadTransactionsMonthUseCase = loadTransactionsMonthUseCase;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadTransaktionenCommand;
    private readonly SearchTransactionsUseCase _searchTransactionsUseCase = searchTransactionsUseCase;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ImportService _importService = importService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly LoadCategoriesUseCase _loadCategoriesUseCase = loadCategoriesUseCase;
    private readonly LoadAccountsUseCase _loadAccountsUseCase = loadAccountsUseCase;
    private readonly IMainThreadDispatcher _dispatcher = dispatcher;
    private readonly IFilePicker _filePicker = filePicker;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<TransactionsViewModel> _logger = logger;
    private readonly IImportSessionStore? _importSessionStore = importSessionStore;
    private readonly LoadTransactionTemplatesUseCase? _loadTransactionTemplatesUseCase = loadTransactionTemplatesUseCase;
    private readonly DeleteTransactionTemplateUseCase? _deleteTransactionTemplateUseCase = deleteTransactionTemplateUseCase;
    private readonly UseTransactionTemplateUseCase? _useTransactionTemplateUseCase = useTransactionTemplateUseCase;
    private readonly Finanzuebersicht.Core.Licensing.ILicenseService _licenseService =
        licenseService ?? Finanzuebersicht.Core.Licensing.UnrestrictedLicenseService.Instance;
    private readonly QuickExpenseCaptureViewModel? _quickExpenseCaptureViewModel = quickExpenseCaptureViewModel;
    private readonly IQuickExpenseCaptureSheetService? _quickExpenseCaptureSheetService = quickExpenseCaptureSheetService;
    private readonly CountUncategorizedTransactionsUseCase? _countUncategorizedTransactionsUseCase = countUncategorizedTransactionsUseCase;
    private readonly IUncategorizedCategoryService? _uncategorizedCategoryService = uncategorizedCategoryService;

    private CancellationTokenSource? _searchDebounce;
    private int _searchVersion;

    private void LogError(string context, Exception? ex = null)
    {
        if (ex != null)
            _logger?.LogError(ex, "TransactionsViewModel: {Context}", context);
        else
            _logger?.LogError("TransactionsViewModel: {Context}", context);
    }

    // --- Monatsansicht ---

    [ObservableProperty]
    private ObservableCollection<TransactionGroup> transaktionsGruppen = [];

    [ObservableProperty]
    private Dictionary<string, string> iconMap = [];

    [ObservableProperty]
    private Dictionary<string, string> accountMap = [];

    [ObservableProperty]
    private Dictionary<string, string> categoryNameMap = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTransactionTemplates))]
    [NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
    private ObservableCollection<TransactionTemplate> transactionTemplates = [];

    public bool HasTransactionTemplates => TransactionTemplates.Count > 0;
    public bool ShowTransactionTemplates => HasTransactionTemplates && IsMonthMode;

    [ObservableProperty]
    private bool isLoading;

    // --- Suche & Filter ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsMonthMode))]
    [NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
    private string searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterActive))]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsMonthMode))]
    [NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
    private string? selectedKategorieId = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterActive))]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsMonthMode))]
    [NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
    private TransactionTypeFilter selectedTypFilter = TransactionTypeFilter.Alle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterActive))]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsMonthMode))]
    [NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
    private DateTime? vonDatum = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterActive))]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsMonthMode))]
    [NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
    private DateTime? bisDatum = null;

    [ObservableProperty]
    private bool isFilterPanelOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    private ObservableCollection<TransactionGroup> searchErgebnisGruppen = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    [NotifyPropertyChangedFor(nameof(SearchCountText))]
    private int totalSearchCount;

    [ObservableProperty]
    private ObservableCollection<KategorieFilterItem> availableKategorien = [];

    [ObservableProperty]
    private ObservableCollection<KategorieFilterItem> availableKonten = [];

    public bool IsFilterActive =>
        SelectedKategorieId != null ||
        SelectedAccountId != null ||
        SelectedTypFilter != TransactionTypeFilter.Alle ||
        IsDateFilterEnabled;

    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText) || IsFilterActive;

    public bool IsMonthMode => !IsSearchActive;

    public bool HasSearchResults => SearchErgebnisGruppen.Count > 0;

    public string SearchCountText => _loc.GetString(ResourceKeys.Lbl_SuchergebnisseAnzahl, TotalSearchCount);

    public string[] TypFilterItems =>
    [
        _loc.GetString(ResourceKeys.Lbl_AlleTypen),
        _loc.GetString(ResourceKeys.Lbl_Einnahmen),
        _loc.GetString(ResourceKeys.Lbl_Ausgaben)
    ];

    // --- Picker-Hilfsfelder ---

    [ObservableProperty]
    private KategorieFilterItem? selectedKategorieFilterItem;

    [ObservableProperty]
    private KategorieFilterItem? selectedKontoFilterItem;

    [ObservableProperty]
    private string? selectedAccountId = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterActive))]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsMonthMode))]
    [NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
    private bool isDateFilterEnabled;

    [ObservableProperty]
    private DateTime vonDatumPicker = new DateTime(DateTime.Today.Year, 1, 1);

    [ObservableProperty]
    private DateTime bisDatumPicker = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUncategorizedTransactions))]
    [NotifyPropertyChangedFor(nameof(UncategorizedBadgeText))]
    private int uncategorizedCount;

    public bool HasUncategorizedTransactions => UncategorizedCount > 0;

    public string UncategorizedBadgeText => UncategorizedCount > 0
        ? _loc.GetString(ResourceKeys.Btn_UnkategorisiertFilter, UncategorizedCount)
        : _loc.GetString(ResourceKeys.Btn_UnkategorisiertFilterZero);

    // Typ-Filter als Index (0=Alle, 1=Einnahmen, 2=Ausgaben) für Picker
    private int _selectedTypIndex;

    [ObservableProperty]
    private string? selectedTypFilterItem;

    public int SelectedTypIndex
    {
        get => _selectedTypIndex;
        set
        {
            if (SetProperty(ref _selectedTypIndex, value))
            {
                SelectedTypFilter = value switch
                {
                    1 => TransactionTypeFilter.Einnahme,
                    2 => TransactionTypeFilter.Ausgabe,
                    _ => TransactionTypeFilter.Alle
                };

                var items = TypFilterItems;
                if (value >= 0 && value < items.Length)
                    SelectedTypFilterItem = items[value];
            }
        }
    }

    partial void OnSelectedTypFilterItemChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var idx = Array.IndexOf(TypFilterItems, value);
        if (idx >= 0 && idx != _selectedTypIndex)
            SelectedTypIndex = idx;
    }

    partial void OnSelectedKategorieFilterItemChanged(KategorieFilterItem? value)
    {
        SelectedKategorieId = value?.Id;
    }

    partial void OnSelectedKontoFilterItemChanged(KategorieFilterItem? value)
    {
        SelectedAccountId = value?.Id;
    }

    partial void OnIsDateFilterEnabledChanged(bool value)
    {
        VonDatum = value ? VonDatumPicker : null;
        BisDatum = value ? BisDatumPicker : null;
        TriggerSearchDebounced();
    }

    partial void OnVonDatumPickerChanged(DateTime value)
    {
        if (IsDateFilterEnabled) VonDatum = value;
    }

    partial void OnBisDatumPickerChanged(DateTime value)
    {
        if (IsDateFilterEnabled) BisDatum = value;
    }

    protected override async Task OnMonthChangedAsync() => await LoadTransaktionen();

    partial void OnSearchTextChanged(string value) => TriggerSearchDebounced();
    partial void OnSelectedKategorieIdChanged(string? value) => TriggerSearchDebounced();
    partial void OnSelectedAccountIdChanged(string? value) => TriggerSearchDebounced();
    partial void OnSelectedTypFilterChanged(TransactionTypeFilter value) => TriggerSearchDebounced();
    partial void OnVonDatumChanged(DateTime? value) => TriggerSearchDebounced();
    partial void OnBisDatumChanged(DateTime? value) => TriggerSearchDebounced();

    private void TriggerSearchDebounced()
    {
        var oldCts = _searchDebounce;
        _searchDebounce = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();
        var token = _searchDebounce.Token;
        var version = Interlocked.Increment(ref _searchVersion);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                    await _dispatcher.InvokeAsync(() => ExecuteSearchAsync(version));
            }
            catch (TaskCanceledException) { }
        });
    }

    private async Task ExecuteSearchAsync(int version = -1)
    {
        if (!IsSearchActive)
        {
            SearchErgebnisGruppen = [];
            TotalSearchCount = 0;
            return;
        }
        IsLoading = true;
        try
        {
            var query = new SearchTransactionsQuery(
                SearchText: SearchText.Trim(),
                KategorieId: SelectedKategorieId,
                AccountId: SelectedAccountId,
                Typ: SelectedTypFilter,
                VonDatum: VonDatum,
                BisDatum: BisDatum);
            var result = await _searchTransactionsUseCase.ExecuteAsync(query);
            if (version >= 0 && version != _searchVersion) return;
            SearchErgebnisGruppen = new ObservableCollection<TransactionGroup>(result.Gruppen);
            TotalSearchCount = result.TotalCount;
            IconMap = result.IconMap;
            CategoryNameMap = result.CategoryNameMap;
            AccountMap = result.AccountMap;
        }
        catch (Exception ex)
        {
            LogError(nameof(ExecuteSearchAsync), ex);
            if (version >= 0 && version != _searchVersion) return;
            SearchErgebnisGruppen = [];
            TotalSearchCount = 0;
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SucheFehlgeschlagen),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        finally
        {
            if (version < 0 || version == _searchVersion)
                IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleFilterPanel() => IsFilterPanelOpen = !IsFilterPanelOpen;

    [RelayCommand]
    private async Task ClearSearch()
    {
        _searchDebounce?.Cancel();
        SearchText = string.Empty;
        SelectedKategorieId = null;
        SelectedKategorieFilterItem = AvailableKategorien.FirstOrDefault();
        SelectedAccountId = null;
        SelectedKontoFilterItem = AvailableKonten.FirstOrDefault();
        SelectedTypFilter = TransactionTypeFilter.Alle;
        SelectedTypIndex = 0;
        IsDateFilterEnabled = false;
        VonDatumPicker = new DateTime(DateTime.Today.Year, 1, 1);
        BisDatumPicker = DateTime.Today;
        VonDatum = null;
        BisDatum = null;
        IsFilterPanelOpen = false;
        SearchErgebnisGruppen = [];
        TotalSearchCount = 0;
        await LoadTransaktionen();
    }

    private async Task LoadKategorienAsync()
    {
        var kategorien = await _loadCategoriesUseCase.ExecuteAsync();
        var items = new ObservableCollection<KategorieFilterItem>
        {
            new(null, _loc.GetString(ResourceKeys.Lbl_AlleKategorien), ResourceKeys.Lbl_AlleKategorien)
        };
        foreach (var k in kategorien.OrderBy(k => k.Name))
            items.Add(new KategorieFilterItem(k.Id, $"{k.Icon} {k.Name}"));
        AvailableKategorien = items;
        SelectedKategorieFilterItem = items[0];
        SelectedTypFilterItem ??= TypFilterItems[SelectedTypIndex];
    }

    private async Task LoadKontenAsync()
    {
        var konten = await _loadAccountsUseCase.ExecuteAsync();
        var items = new ObservableCollection<KategorieFilterItem>
        {
            new(null, _loc.GetString(ResourceKeys.Lbl_AlleKonten), ResourceKeys.Lbl_AlleKonten)
        };
        foreach (var konto in konten.OrderBy(k => k.Name))
            items.Add(new KategorieFilterItem(konto.Id, konto.Name));
        AvailableKonten = items;
        SelectedKontoFilterItem = items.FirstOrDefault(i => i.Id == SelectedAccountId) ?? items[0];
    }

    [RelayCommand]
    private async Task LoadTransaktionen()
    {
        CurrencyRefreshRegistry.Register(this);
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var data = await _loadTransactionsMonthUseCase.ExecuteAsync(AktuellerMonat, SelectedAccountId);
            TransaktionsGruppen = new ObservableCollection<TransactionGroup>(data.Gruppen);
            IconMap = data.IconMap;
            CategoryNameMap = data.CategoryNameMap;
            AccountMap = data.AccountMap;

            if (AvailableKategorien.Count == 0)
                await LoadKategorienAsync();
            if (AvailableKonten.Count == 0)
                await LoadKontenAsync();

            await LoadTemplatesAsync();
            await RefreshUncategorizedCountAsync();
        }
        catch (Exception ex)
        {
            LogError(nameof(LoadTransaktionen), ex);
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

    private async Task RefreshUncategorizedCountAsync()
    {
        if (_countUncategorizedTransactionsUseCase == null)
        {
            UncategorizedCount = 0;
            return;
        }

        try
        {
            UncategorizedCount = await _countUncategorizedTransactionsUseCase.ExecuteAsync();
        }
        catch (Exception ex)
        {
            LogError(nameof(RefreshUncategorizedCountAsync), ex);
            UncategorizedCount = 0;
        }
    }

    private async Task LoadTemplatesAsync()
    {
        if (_loadTransactionTemplatesUseCase == null) return;

        var templates = await _loadTransactionTemplatesUseCase.ExecuteAsync();
        TransactionTemplates = new ObservableCollection<TransactionTemplate>(templates);
    }

    [RelayCommand]
    private async Task DeleteTransaktion(Transaction transaktion)
    {
        var dialogTitle = transaktion.IsTransfer
            ? _loc.GetString(ResourceKeys.Dlg_UmbuchungLoeschen)
            : _loc.GetString(ResourceKeys.Dlg_TransaktionLoeschen);
        var dialogText = transaktion.IsTransfer
            ? _loc.GetString(ResourceKeys.Dlg_UmbuchungLoeschenFrage, transaktion.Titel)
            : _loc.GetString(ResourceKeys.Dlg_TransaktionLoeschenFrage, transaktion.Titel);
        var confirm = await _dialogService.ShowConfirmationAsync(
            dialogTitle,
            dialogText,
            _loc.GetString(ResourceKeys.Btn_Ja), _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            if (transaktion.IsTransfer && !string.IsNullOrWhiteSpace(transaktion.TransferGroupId))
            {
                await _deleteTransactionUseCase.ExecuteTransferGroupAsync(transaktion.TransferGroupId);
                await LoadTransaktionen();
                _appEvents.NotifyDataChanged();
                await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
                return;
            }

            var snapshot = CloneTransaction(transaktion);
            await _deleteTransactionUseCase.ExecuteAsync(transaktion.Id);
            var gruppe = TransaktionsGruppen.FirstOrDefault(g => g.Contains(transaktion));
            if (gruppe == null) return;

            gruppe.Remove(transaktion);
            if (gruppe.Count == 0)
                TransaktionsGruppen.Remove(gruppe);

            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(
                _loc.GetString(ResourceKeys.Msg_Geloescht),
                _loc.GetString(ResourceKeys.Btn_Rueckgaengig),
                async () =>
                {
                    await _restoreTransactionUseCase.ExecuteAsync(snapshot);
                    await LoadTransaktionen();
                    _appEvents.NotifyDataChanged();
                });
        }
        catch (Exception ex)
        {
            LogError($"DeleteTransaktion failed for {transaktion.Id}", ex);
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task GoToDetail(Transaction? transaktion)
    {
        try
        {
            if (transaktion?.IsTransfer == true)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Err_Titel),
                    _loc.GetString(ResourceKeys.Err_UmbuchungNichtBearbeitbar),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            _logger?.LogDebug("GoToDetail called for transaction {Id}", transaktion?.Id ?? "(new)");

            var parameter = new Dictionary<string, object>();
            if (transaktion != null)
            {
                parameter[NavigationQueryKeys.Transaction] = transaktion;
            }

            if (_navigationService == null)
            {
                LogError("GoToDetail: navigation service is null");
                return;
            }

            await _navigationService.GoToAsync(Routes.TransactionDetail, parameter);
        }
        catch (Exception ex)
        {
            LogError("GoToDetail failed", ex);
        }
    }

    [RelayCommand]
    private async Task DuplicateTransaktion(Transaction transaktion)
    {
        if (transaktion == null) return;

        await _navigationService.GoToAsync(Routes.TransactionDetail, new Dictionary<string, object>
        {
            [NavigationQueryKeys.DuplicateTransaction] = transaktion
        });
    }

    [RelayCommand]
    private async Task CreateFromTemplate(TransactionTemplate template)
    {
        if (template == null) return;

        if (_useTransactionTemplateUseCase != null)
        {
            await _useTransactionTemplateUseCase.ExecuteAsync(template);
            await LoadTemplatesAsync();
        }

        await _navigationService.GoToAsync(Routes.TransactionDetail, new Dictionary<string, object>
        {
            [NavigationQueryKeys.TransactionTemplate] = template
        });
    }

    [RelayCommand]
    private async Task GoToTransfer()
    {
        await _navigationService.GoToAsync(Routes.TransferDetail);
    }

    [RelayCommand]
    private async Task QuickCapture()
    {
        if (_quickExpenseCaptureViewModel == null || _quickExpenseCaptureSheetService == null)
            return;

        if (!_quickExpenseCaptureViewModel.EnsureProAccess())
        {
            await _quickExpenseCaptureViewModel.ShowProRequiredAsync();
            return;
        }

        _quickExpenseCaptureViewModel.Reset();
        if (await _quickExpenseCaptureSheetService.ShowAsync(_quickExpenseCaptureViewModel))
        {
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_SchnellAusgabeGespeichert));
            await LoadKategorienAsync();
            if (IsSearchActive)
                await ExecuteSearchAsync();
            else
                await LoadTransaktionen();
        }
    }

    [RelayCommand]
    private async Task FilterUncategorized()
    {
        if (_uncategorizedCategoryService == null)
            return;

        var id = await _uncategorizedCategoryService.FindIdAsync();
        if (string.IsNullOrWhiteSpace(id))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Msg_KeineUnkategorisierten),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        IsFilterPanelOpen = true;
        SelectedKategorieFilterItem = AvailableKategorien.FirstOrDefault(k => k.Id == id)
            ?? new KategorieFilterItem(id, _loc.GetString(ResourceKeys.Lbl_ImportStatusUnkategorisiert));
        SelectedKategorieId = id;
    }

    [RelayCommand]
    private async Task DeleteTemplate(TransactionTemplate template)
    {
        if (template == null || _deleteTransactionTemplateUseCase == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_VorlageLoeschen),
            _loc.GetString(ResourceKeys.Dlg_VorlageLoeschenFrage, template.Name),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        await _deleteTransactionTemplateUseCase.ExecuteAsync(template.Id);
        await LoadTemplatesAsync();
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        if (!_licenseService.HasFeature(Finanzuebersicht.Core.Licensing.AppFeature.CsvImport))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_ProErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        // Defensive checks to avoid NullReferenceExceptions when DI failed
        if (_importService == null)
        {
            var title = _loc?.GetString(Finanzuebersicht.Resources.Strings.ResourceKeys.Msg_ImportFehlgeschlagen_Title) ?? "Import fehlgeschlagen";
            var msg = _loc?.GetString(Finanzuebersicht.Resources.Strings.ResourceKeys.Msg_ImportServiceNichtVerfuegbar) ?? "ImportService nicht verfügbar.";
            var ok = _loc?.GetString(Finanzuebersicht.Resources.Strings.ResourceKeys.Btn_OK) ?? "OK";

            if (_dialogService != null)
            {
                await _dialogService.ShowAlertAsync(title, msg, ok);
            }
            else
            {
                LogError("ImportCsv: DialogService is null while handling missing ImportService");
            }

            return;
        }

        try
        {
            var result = await _filePicker.PickAsync();
            if (result == null) return;

            using var stream = await result.OpenReadAsync();
            var preview = await _importService.AnalyzeCsvAsync(stream, SelectedAccountId);

            if (!preview.Success)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_ImportFehlgeschlagen_Title),
                    preview.ErrorMessage ?? "Unbekannter Fehler beim Import.",
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            if (_importSessionStore == null)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtVerfuegbar_Title),
                    _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtVerfuegbar_Body),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            _importSessionStore.Clear();
            _importSessionStore.SetActiveSession(preview);
            await _navigationService.GoToAsync(Routes.ImportPreview);
        }
        catch (System.Exception ex)
        {
            // Log full exception for debugging
            try
            {
                _logger?.LogError(ex, "ImportCsv failed");
            }
            catch { /* swallow logger exceptions */ }

            // Ensure we don't call a null dialog service in the catch
            var msg = ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : string.Empty);
            var errTitle = _loc?.GetString(Finanzuebersicht.Resources.Strings.ResourceKeys.Msg_ImportFehler_Title) ?? "Fehler beim Import";
            var okError = _loc?.GetString(Finanzuebersicht.Resources.Strings.ResourceKeys.Btn_OK) ?? "OK";

            await _dialogService.ShowAlertAsync(errTitle, msg, okError);
        }
    }

    public void RefreshCurrencyDisplay()
    {
        CurrencyDisplayRefresh.RebindTransactionGroups(TransaktionsGruppen);
        CurrencyDisplayRefresh.RebindTransactionGroups(SearchErgebnisGruppen);
        if (TransactionTemplates.Count > 0)
            TransactionTemplates = CurrencyDisplayRefresh.Clone(TransactionTemplates);
    }

    private static Transaction CloneTransaction(Transaction source) => new()
    {
        Id = source.Id,
        Betrag = source.Betrag,
        Titel = source.Titel,
        Datum = source.Datum,
        KategorieId = source.KategorieId,
        Typ = source.Typ,
        DauerauftragId = source.DauerauftragId,
        AccountId = source.AccountId,
        IsTransfer = source.IsTransfer,
        TransferGroupId = source.TransferGroupId,
        Verwendungszweck = source.Verwendungszweck,
        SparZielId = source.SparZielId
    };
}
