using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class ImportPreviewViewModel(
    CommitCsvImportUseCase commitCsvImportUseCase,
    IImportSessionStore importSessionStore,
    ICategoryRepository categoryRepository,
    INavigationService navigationService,
    IDialogService dialogService,
    ILocalizationService localizationService,
    IAppEvents appEvents,
    IFeedbackService feedbackService,
    ILogger<ImportPreviewViewModel>? logger = null) : ObservableObject, IAutoLoadViewModel, ILocalizableViewModel
{
    private readonly CommitCsvImportUseCase _commitCsvImportUseCase = commitCsvImportUseCase;
    private readonly IImportSessionStore _importSessionStore = importSessionStore;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly ILogger<ImportPreviewViewModel>? _logger = logger;

    private ImportPreviewResult? _activeSession;
    private bool _loadedPreview;
    private List<ImportPreviewCategoryOption> _categoryOptions = [];

    public System.Windows.Input.ICommand AutoLoadCommand => LoadPreviewCommand;
    public bool ShouldAutoLoad => !_loadedPreview;

    [ObservableProperty]
    private ObservableCollection<ImportPreviewRowItemViewModel> rows = [];

    [ObservableProperty]
    private ObservableCollection<ImportPreviewRowItemViewModel> filteredRows = [];

    [ObservableProperty]
    private ObservableCollection<ImportPreviewFilterOption> filterOptions = [];

    [ObservableProperty]
    private ImportPreviewFilterOption? selectedFilter;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasRows;

    [ObservableProperty]
    private string summaryText = string.Empty;

    partial void OnSelectedFilterChanged(ImportPreviewFilterOption? value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadPreview()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            _activeSession = _importSessionStore.GetActiveSession();
            if (_activeSession is null)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtVerfuegbar_Title),
                    _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtVerfuegbar_Body),
                    _loc.GetString(ResourceKeys.Btn_OK));
                await _navigationService.GoBackAsync();
                return;
            }

            var categories = await _categoryRepository.GetCategoriesAsync();
            _categoryOptions =
            [
                new ImportPreviewCategoryOption(string.Empty, _loc.GetString(ResourceKeys.Lbl_OhneKategorie)),
                .. categories
                    .OrderBy(c => c.Name)
                    .Select(c => new ImportPreviewCategoryOption(c.Id, $"{c.Icon} {c.Name}"))
            ];

            Rows = new ObservableCollection<ImportPreviewRowItemViewModel>(
                _activeSession.Rows.Select(row =>
                    new ImportPreviewRowItemViewModel(row, _categoryOptions, _loc, OnRowsChanged)));

            _loadedPreview = true;
            RefreshState();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ImportPreviewViewModel: failed to load preview");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                ex.Message,
                _loc.GetString(ResourceKeys.Btn_OK));
            await _navigationService.GoBackAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CommitImport()
    {
        if (_activeSession is null)
            return;

        var selectedRowIds = Rows
            .Where(r => r.IsIncluded && r.CanCommit)
            .Select(r => r.Id)
            .ToList();

        if (selectedRowIds.Count == 0)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtsAusgewaehlt_Title),
                _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtsAusgewaehlt_Body),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            var result = await _commitCsvImportUseCase.ExecuteAsync(_activeSession, selectedRowIds);
            var summaryLines = new List<string>
            {
                string.Format(_loc.GetString(ResourceKeys.Msg_ImportiertCount), result.Imported.Count)
            };

            if (result.Duplicates.Count > 0)
                summaryLines.Add(string.Format(_loc.GetString(ResourceKeys.Msg_ImportDuplikateCount), result.Duplicates.Count));
            if (result.SaveErrors.Count > 0)
                summaryLines.Add(string.Format(_loc.GetString(ResourceKeys.Msg_ImportFehlerCount), result.SaveErrors.Count));

            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(string.Join(" · ", summaryLines));

            _importSessionStore.Clear();
            _loadedPreview = false;
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ImportPreviewViewModel: commit failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_ImportFehler_Title),
                ex.Message,
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task CancelImport()
    {
        _importSessionStore.Clear();
        _loadedPreview = false;
        await _navigationService.GoBackAsync();
    }

    public void HandlePageDisappearing()
    {
        _importSessionStore.Clear();
        _loadedPreview = false;
    }

    private void OnRowsChanged() => RefreshState();

    private void RefreshState()
    {
        RefreshFilterOptions();
        ApplyFilter();
        // HasRows should reflect currently visible rows after filtering
        HasRows = FilteredRows.Count > 0;
        SummaryText = string.Join(" · ",
            string.Format(_loc.GetString(ResourceKeys.Lbl_ImportBereitCount), Rows.Count(r => r.Status == ImportPreviewRowStatus.Ready)),
            string.Format(_loc.GetString(ResourceKeys.Lbl_ImportDuplikateCount), Rows.Count(r => r.Status == ImportPreviewRowStatus.Duplicate)),
            string.Format(_loc.GetString(ResourceKeys.Lbl_ImportProblemeCount), Rows.Count(r => r.IsProblem)),
            string.Format(_loc.GetString(ResourceKeys.Lbl_ImportAusgewaehltCount), Rows.Count(r => r.IsIncluded && r.CanCommit)));
    }

    private void RefreshFilterOptions()
    {
        var selectedFilterKind = SelectedFilter?.Filter ?? ImportPreviewFilter.All;
        FilterOptions =
        [
            new ImportPreviewFilterOption(ImportPreviewFilter.All, _loc.GetString(ResourceKeys.Lbl_ImportFilterAlle)),
            new ImportPreviewFilterOption(ImportPreviewFilter.Ready, _loc.GetString(ResourceKeys.Lbl_ImportFilterBereit)),
            new ImportPreviewFilterOption(ImportPreviewFilter.Duplicates, _loc.GetString(ResourceKeys.Lbl_ImportFilterDuplikate)),
            new ImportPreviewFilterOption(ImportPreviewFilter.Problems, _loc.GetString(ResourceKeys.Lbl_ImportFilterProbleme)),
            new ImportPreviewFilterOption(ImportPreviewFilter.Excluded, _loc.GetString(ResourceKeys.Lbl_ImportFilterAusgeschlossen))
        ];

        // Reset SelectedFilter to instance from new list to ensure Picker shows selection
        var newSelected = FilterOptions.FirstOrDefault(f => f.Filter == selectedFilterKind) ?? FilterOptions.FirstOrDefault();
        SelectedFilter = null;
        SelectedFilter = newSelected;
    }

    private void ApplyFilter()
    {
        IEnumerable<ImportPreviewRowItemViewModel> filtered = Rows;
        switch (SelectedFilter?.Filter ?? ImportPreviewFilter.All)
        {
            case ImportPreviewFilter.Ready:
                filtered = Rows.Where(r => r.Status == ImportPreviewRowStatus.Ready || r.Status == ImportPreviewRowStatus.Uncategorized);
                break;
            case ImportPreviewFilter.Duplicates:
                filtered = Rows.Where(r => r.Status == ImportPreviewRowStatus.Duplicate);
                break;
            case ImportPreviewFilter.Problems:
                filtered = Rows.Where(r => r.IsProblem);
                break;
            case ImportPreviewFilter.Excluded:
                filtered = Rows.Where(r => !r.IsIncluded);
                break;
        }

        FilteredRows = new ObservableCollection<ImportPreviewRowItemViewModel>(filtered);
    }

    public void RefreshLocalizedStrings()
    {
        RefreshFilterOptions();
        RefreshState();
        foreach (var row in Rows)
            row.RefreshLocalizedStrings();
    }
}

public partial class ImportPreviewRowItemViewModel : ObservableObject
{
    private readonly ImportPreviewRow _row;
    private readonly Action _onChanged;
    private readonly ILocalizationService _loc;

    public ImportPreviewRowItemViewModel(
        ImportPreviewRow row,
        IReadOnlyList<ImportPreviewCategoryOption> categoryOptions,
        ILocalizationService localizationService,
        Action onChanged)
    {
        _row = row;
        _loc = localizationService;
        _onChanged = onChanged;
        CategoryOptions = new ObservableCollection<ImportPreviewCategoryOption>(
            categoryOptions.Select(option => new ImportPreviewCategoryOption(option.Id, option.DisplayName)));
        status = row.Status;
        isIncluded = row.IsIncluded;
        selectedCategoryOption = CategoryOptions.FirstOrDefault(c => c.Id == row.Transaction.KategorieId)
            ?? CategoryOptions.FirstOrDefault();
    }

    public string Id => _row.Id;
    public ObservableCollection<ImportPreviewCategoryOption> CategoryOptions { get; }
    public string Title => _row.Transaction.Titel;
    public string Purpose => _row.Transaction.Verwendungszweck;
    public string DateText => _row.Transaction.Datum == default ? "—" : _row.Transaction.Datum.ToString("d", CultureInfo.CurrentCulture);
    public string AmountText
    {
        get
        {
            var tx = _row.Transaction;
            if (tx == null) return "—";
            var ci = CurrencyCulture.Instance;
            var abs = Math.Abs(tx.Betrag);
            var formatted = abs.ToString("C", ci);
            var sign = tx.Typ == TransactionType.Einnahme ? "+" : "-";
            return $"{sign}{formatted}";
        }
    }
    public bool CanCommit => Status is ImportPreviewRowStatus.Ready or ImportPreviewRowStatus.Uncategorized or ImportPreviewRowStatus.SaveError;
    public bool IsProblem => Status is ImportPreviewRowStatus.Duplicate or ImportPreviewRowStatus.Invalid or ImportPreviewRowStatus.Uncategorized;
    public bool CanEditCategory => Status != ImportPreviewRowStatus.Invalid;

    [ObservableProperty]
    private bool isIncluded;

    [ObservableProperty]
    private ImportPreviewRowStatus status;

    [ObservableProperty]
    private ImportPreviewCategoryOption? selectedCategoryOption;

    public string StatusText => Status switch
    {
        ImportPreviewRowStatus.Ready => _loc.GetString(ResourceKeys.Lbl_ImportStatusBereit),
        ImportPreviewRowStatus.Duplicate => _loc.GetString(ResourceKeys.Lbl_ImportStatusDuplikat),
        ImportPreviewRowStatus.Invalid => _loc.GetString(ResourceKeys.Lbl_ImportStatusUngueltig),
        ImportPreviewRowStatus.Uncategorized => _loc.GetString(ResourceKeys.Lbl_ImportStatusUnkategorisiert),
        ImportPreviewRowStatus.SaveError => _loc.GetString(ResourceKeys.Lbl_ImportStatusSpeicherfehler),
        _ => _loc.GetString(ResourceKeys.Lbl_ImportStatusUnbekannt)
    };

    public void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(AmountText));
        OnPropertyChanged(nameof(DateText));
    }

    partial void OnIsIncludedChanged(bool value)
    {
        _row.IsIncluded = value;
        _onChanged();
    }

    partial void OnStatusChanged(ImportPreviewRowStatus value)
    {
        _row.Status = value;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(CanCommit));
        OnPropertyChanged(nameof(IsProblem));
    }

    partial void OnSelectedCategoryOptionChanged(ImportPreviewCategoryOption? value)
    {
        if (value is null)
            return;

        _row.Transaction.KategorieId = value.Id;

        if (Status is not ImportPreviewRowStatus.Invalid and not ImportPreviewRowStatus.Duplicate)
        {
            Status = string.IsNullOrWhiteSpace(value.Id)
                ? ImportPreviewRowStatus.Uncategorized
                : ImportPreviewRowStatus.Ready;
        }

        _onChanged();
    }
}

public enum ImportPreviewFilter
{
    All,
    Ready,
    Duplicates,
    Problems,
    Excluded
}

public record ImportPreviewFilterOption(ImportPreviewFilter Filter, string DisplayName);

public record ImportPreviewCategoryOption(string Id, string DisplayName);
