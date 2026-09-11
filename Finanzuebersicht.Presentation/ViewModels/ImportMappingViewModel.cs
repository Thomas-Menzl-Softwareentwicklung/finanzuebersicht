using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class ImportMappingViewModel : ObservableObject, IAutoLoadViewModel, ILocalizableViewModel
{
    private static readonly string[] DateFormats =
        ["dd.MM.yyyy", "dd.MM.yy", "yyyy-MM-dd", "dd/MM/yyyy", "d.M.yyyy"];

    private readonly AnalyzeCsvImportUseCase _analyzeCsvImportUseCase;
    private readonly ICsvImportProfileStore _profileStore;
    private readonly IImportSessionStore _importSessionStore;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _loc;
    private readonly ILogger<ImportMappingViewModel>? _logger;

    private CsvTable? _table;
    private bool _openedFromPreview;
    private bool _loaded;
    private bool _suppressSelectionUpdates;
    private bool _leaveWithoutClearing;

    public ImportMappingViewModel(
        AnalyzeCsvImportUseCase analyzeCsvImportUseCase,
        ICsvImportProfileStore profileStore,
        IImportSessionStore importSessionStore,
        INavigationService navigationService,
        IDialogService dialogService,
        ILocalizationService localizationService,
        ILogger<ImportMappingViewModel>? logger = null)
    {
        _analyzeCsvImportUseCase = analyzeCsvImportUseCase;
        _profileStore = profileStore;
        _importSessionStore = importSessionStore;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _loc = localizationService;
        _logger = logger;
    }

    public System.Windows.Input.ICommand AutoLoadCommand => LoadMappingCommand;
    public bool ShouldAutoLoad => !_loaded;

    [ObservableProperty]
    private ObservableCollection<string> previewRows = [];

    [ObservableProperty]
    private ObservableCollection<CsvHeaderRowOption> headerRowOptions = [];

    [ObservableProperty]
    private CsvHeaderRowOption? selectedHeaderRow;

    [ObservableProperty]
    private ObservableCollection<CsvHeaderOption> columnOptions = [];

    [ObservableProperty]
    private CsvHeaderOption? selectedDate;

    [ObservableProperty]
    private CsvHeaderOption? selectedAmount;

    [ObservableProperty]
    private CsvHeaderOption? selectedTitle;

    [ObservableProperty]
    private CsvHeaderOption? selectedPurpose;

    [ObservableProperty]
    private CsvHeaderOption? selectedAmountSign;

    [ObservableProperty]
    private CsvHeaderOption? selectedIban;

    [ObservableProperty]
    private ObservableCollection<CsvDateFormatOption> dateFormatOptions = [];

    [ObservableProperty]
    private CsvDateFormatOption? selectedDateFormat;

    [ObservableProperty]
    private ObservableCollection<CsvDecimalStyleOption> decimalStyleOptions = [];

    [ObservableProperty]
    private CsvDecimalStyleOption? selectedDecimalStyle;

    [ObservableProperty]
    private bool canContinue;

    [RelayCommand]
    private async Task LoadMapping()
    {
        _openedFromPreview = _importSessionStore.GetActiveSession() is not null;
        _table = _importSessionStore.GetTable();
        if (_table is null)
        {
            await _navigationService.GoBackAsync();
            return;
        }

        _suppressSelectionUpdates = true;
        try
        {
            BuildStaticOptions();
            PreviewRows = new ObservableCollection<string>(
                _table.AllRows.Take(5).Select(row => string.Join(_table.Delimiter.ToString(), row)));
            HeaderRowOptions = new ObservableCollection<CsvHeaderRowOption>(
                _table.AllRows.Select((row, index) => new CsvHeaderRowOption(
                    index,
                    $"{index + 1}: {string.Join(" | ", row.Take(4))}")));
            SelectedHeaderRow = HeaderRowOptions.FirstOrDefault(o => o.Index == _table.HeaderRowIndex)
                                ?? HeaderRowOptions.FirstOrDefault();
            RebuildColumnOptions();
            ApplyExistingProfileOrGuess();
            RefreshCanContinue();
            _loaded = true;
        }
        finally
        {
            _suppressSelectionUpdates = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private async Task Continue()
    {
        if (_table is null || !CanContinue)
            return;

        var profile = BuildProfile();
        try
        {
            await _profileStore.UpsertAsync(profile);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ImportMappingViewModel: profile upsert failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Msg_ImportProfilSpeichernFehlgeschlagen),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            _importSessionStore.SetTable(_table, _importSessionStore.GetAccountId());
            var preview = await _analyzeCsvImportUseCase.ExecuteAsync(
                _table, profile, _importSessionStore.GetAccountId());
            if (!preview.Success)
            {
                var errorDetail = string.IsNullOrWhiteSpace(preview.ErrorMessage)
                    ? "Unbekannter Fehler beim Import."
                    : _loc.GetString(preview.ErrorMessage);
                if (string.IsNullOrWhiteSpace(errorDetail) || errorDetail == preview.ErrorMessage)
                    errorDetail = preview.ErrorMessage ?? errorDetail;
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_ImportFehlgeschlagen_Title),
                    errorDetail,
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            _importSessionStore.SetActiveSession(preview, profile);
            _leaveWithoutClearing = true;
            if (_openedFromPreview)
                await _navigationService.GoBackAsync();
            else
                await _navigationService.GoToAsync(Routes.ImportPreview);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ImportMappingViewModel: analyze failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_ImportFehler_Title),
                ex.Message,
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        if (!_openedFromPreview)
            _importSessionStore.Clear();
        _leaveWithoutClearing = true;
        await _navigationService.GoBackAsync();
    }

    public void HandlePageDisappearing()
    {
        if (_leaveWithoutClearing || _openedFromPreview)
            return;

        _importSessionStore.Clear();
    }

    public void RefreshLocalizedStrings()
    {
        if (_table is null)
            return;

        _suppressSelectionUpdates = true;
        try
        {
            BuildStaticOptions();
            RebuildColumnOptions();
            ApplyExistingProfileOrGuess();
            RefreshCanContinue();
        }
        finally
        {
            _suppressSelectionUpdates = false;
        }
    }

    partial void OnSelectedHeaderRowChanged(CsvHeaderRowOption? value)
    {
        if (_suppressSelectionUpdates || _table is null || value is null)
            return;

        _table = _table.WithHeaderRow(value.Index);
        _suppressSelectionUpdates = true;
        try
        {
            RebuildColumnOptions();
            ApplyGuess();
            RefreshCanContinue();
        }
        finally
        {
            _suppressSelectionUpdates = false;
        }
    }

    partial void OnSelectedDateChanged(CsvHeaderOption? value)
    {
        if (_suppressSelectionUpdates)
            return;
        DetectFormatsFromSelection();
        RefreshCanContinue();
    }

    partial void OnSelectedAmountChanged(CsvHeaderOption? value)
    {
        if (_suppressSelectionUpdates)
            return;
        DetectFormatsFromSelection();
        RefreshCanContinue();
    }

    partial void OnSelectedTitleChanged(CsvHeaderOption? value)
    {
        if (!_suppressSelectionUpdates)
            RefreshCanContinue();
    }

    partial void OnSelectedPurposeChanged(CsvHeaderOption? value)
    {
        if (!_suppressSelectionUpdates)
            RefreshCanContinue();
    }

    partial void OnCanContinueChanged(bool value) => ContinueCommand.NotifyCanExecuteChanged();

    private void BuildStaticOptions()
    {
        DateFormatOptions = new ObservableCollection<CsvDateFormatOption>(
            DateFormats.Select(format => new CsvDateFormatOption(format, format)));
        DecimalStyleOptions =
        [
            new CsvDecimalStyleOption(CsvDecimalStyle.Comma, _loc.GetString(ResourceKeys.Lbl_ImportDezimalKomma)),
            new CsvDecimalStyleOption(CsvDecimalStyle.Point, _loc.GetString(ResourceKeys.Lbl_ImportDezimalPunkt))
        ];
        SelectedDateFormat ??= DateFormatOptions.FirstOrDefault();
        SelectedDecimalStyle ??= DecimalStyleOptions.FirstOrDefault();
    }

    private void RebuildColumnOptions()
    {
        if (_table is null)
            return;

        var unused = new CsvHeaderOption(null, _loc.GetString(ResourceKeys.Lbl_ImportSpalteKeine));
        ColumnOptions = new ObservableCollection<CsvHeaderOption>(
        [
            unused,
            .. _table.Headers.Select(header => new CsvHeaderOption(header, header))
        ]);
    }

    private void ApplyExistingProfileOrGuess()
    {
        var existing = _importSessionStore.GetProfile();
        if (existing is not null)
            ApplyProfile(existing);
        else
            ApplyGuess();
    }

    private void ApplyProfile(CsvImportProfile profile)
    {
        if (_table is null)
            return;

        if (profile.HeaderRowIndex != _table.HeaderRowIndex
            && profile.HeaderRowIndex >= 0
            && profile.HeaderRowIndex < _table.AllRows.Count)
        {
            _table = _table.WithHeaderRow(profile.HeaderRowIndex);
            SelectedHeaderRow = HeaderRowOptions.FirstOrDefault(o => o.Index == profile.HeaderRowIndex);
            RebuildColumnOptions();
        }

        SelectedDate = FindColumn(profile.Columns.Date);
        SelectedAmount = FindColumn(profile.Columns.Amount);
        SelectedTitle = FindColumn(profile.Columns.Title);
        SelectedPurpose = FindColumn(profile.Columns.Purpose);
        SelectedAmountSign = FindColumn(profile.Columns.AmountSign);
        SelectedIban = FindColumn(profile.Columns.Iban);
        SelectedDateFormat = DateFormatOptions.FirstOrDefault(o => o.Format == profile.DateFormat)
                             ?? DateFormatOptions.FirstOrDefault();
        SelectedDecimalStyle = DecimalStyleOptions.FirstOrDefault(o => o.Style == profile.DecimalStyle)
                               ?? DecimalStyleOptions.FirstOrDefault();
    }

    private void ApplyGuess()
    {
        if (_table is null)
            return;

        var mapping = CsvMappingGuesser.Guess(_table);
        SelectedDate = FindColumn(mapping.Date);
        SelectedAmount = FindColumn(mapping.Amount);
        SelectedTitle = FindColumn(mapping.Title);
        SelectedPurpose = FindColumn(mapping.Purpose);
        SelectedAmountSign = FindColumn(mapping.AmountSign);
        SelectedIban = FindColumn(mapping.Iban);
        DetectFormatsFromSelection();
    }

    private void DetectFormatsFromSelection()
    {
        if (_table is null)
            return;

        if (SelectedDate?.Header is not null)
        {
            var samples = ColumnSamples(SelectedDate.Header);
            var format = CsvFormatDetector.DetectDateFormat(samples);
            SelectedDateFormat = DateFormatOptions.FirstOrDefault(o => o.Format == format)
                                 ?? DateFormatOptions.FirstOrDefault();
        }

        if (SelectedAmount?.Header is not null)
        {
            var samples = ColumnSamples(SelectedAmount.Header);
            var style = CsvFormatDetector.DetectDecimalStyle(samples);
            SelectedDecimalStyle = DecimalStyleOptions.FirstOrDefault(o => o.Style == style)
                                   ?? DecimalStyleOptions.FirstOrDefault();
        }
    }

    private IEnumerable<string> ColumnSamples(string header)
    {
        if (_table is null)
            return [];

        var index = -1;
        for (var i = 0; i < _table.Headers.Count; i++)
        {
            if (string.Equals(_table.Headers[i], header, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return [];

        return _table.DataRows
            .Select(row => index < row.Count ? row[index] : string.Empty)
            .Take(25);
    }

    private CsvHeaderOption FindColumn(string? header)
        => ColumnOptions.FirstOrDefault(o => o.Header == header)
           ?? ColumnOptions.FirstOrDefault()
           ?? new CsvHeaderOption(null, _loc.GetString(ResourceKeys.Lbl_ImportSpalteKeine));

    private void RefreshCanContinue()
    {
        CanContinue = SelectedDate?.Header is not null
                      && SelectedAmount?.Header is not null
                      && (SelectedTitle?.Header is not null || SelectedPurpose?.Header is not null);
    }

    private CsvImportProfile BuildProfile()
    {
        var existing = _importSessionStore.GetProfile();
        var id = existing is { IsBuiltIn: false } && !string.IsNullOrWhiteSpace(existing.Id)
            ? existing.Id
            : Guid.NewGuid().ToString();

        return new CsvImportProfile
        {
            Id = id,
            Name = existing is { IsBuiltIn: false } ? existing.Name : string.Empty,
            IsBuiltIn = false,
            Delimiter = _table!.Delimiter,
            Headers = _table.Headers.ToList(),
            HeaderRowIndex = _table.HeaderRowIndex,
            Columns = new CsvColumnMapping
            {
                Date = SelectedDate?.Header,
                Amount = SelectedAmount?.Header,
                Title = SelectedTitle?.Header,
                Purpose = SelectedPurpose?.Header,
                AmountSign = SelectedAmountSign?.Header,
                Iban = SelectedIban?.Header
            },
            DateFormat = SelectedDateFormat?.Format ?? "dd.MM.yyyy",
            DecimalStyle = SelectedDecimalStyle?.Style ?? CsvDecimalStyle.Comma
        };
    }
}

public sealed record CsvHeaderOption(string? Header, string DisplayName);

public sealed record CsvHeaderRowOption(int Index, string DisplayName);

public sealed record CsvDateFormatOption(string Format, string DisplayName);

public sealed record CsvDecimalStyleOption(CsvDecimalStyle Style, string DisplayName);
