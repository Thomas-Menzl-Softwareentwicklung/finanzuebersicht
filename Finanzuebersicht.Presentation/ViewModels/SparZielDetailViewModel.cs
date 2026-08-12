using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class SparZielDetailViewModel(
    SaveSparZielUseCase saveSparZielUseCase,
    DeleteSparZielUseCase deleteSparZielUseCase,
    LoadSparZieleUseCase loadSparZieleUseCase,
    INavigationService navigationService,
    ILocalizationService localizationService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<SparZielDetailViewModel>? logger = null) : ObservableObject, IApplyQueryAttributes, IAutoLoadViewModel, ILocalizableViewModel
{
    private readonly SaveSparZielUseCase _saveSparZielUseCase = saveSparZielUseCase;
    private readonly DeleteSparZielUseCase _deleteSparZielUseCase = deleteSparZielUseCase;
    private readonly LoadSparZieleUseCase _loadSparZieleUseCase = loadSparZieleUseCase;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<SparZielDetailViewModel>? _logger = logger;

    private SparZiel? _sparZiel;

    [ObservableProperty]
    private string titel = string.Empty;

    [ObservableProperty]
    private string icon = "🎯";

    [ObservableProperty]
    private string zielBetragText = string.Empty;

    [ObservableProperty]
    private string aktuellerBetragText = string.Empty;

    [ObservableProperty]
    private string monatlicheSparrateText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFaelligkeit))]
    private bool useFaelligkeit;

    [ObservableProperty]
    private DateTime faelligkeitsdatum = DateTime.Today;

    [ObservableProperty]
    private decimal fortschrittProzent;

    [ObservableProperty]
    private string fortschrittText = string.Empty;

    public bool HasFaelligkeit => UseFaelligkeit;

    public bool IsEditing => _sparZiel != null;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadProgressCommand;

    public string PageTitle => _loc.GetString(
        IsEditing ? ResourceKeys.Title_SparZielBearbeiten : ResourceKeys.Btn_NeuesZiel);

    public void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(PageTitle));
        if (IsEditing)
            _ = LoadProgressAsync();
    }

    public SparZiel? SparZiel
    {
        set
        {
            if (value == null) return;
            _sparZiel = value;
            Titel = value.Titel;
            Icon = string.IsNullOrWhiteSpace(value.Icon) ? "🎯" : value.Icon;
            ZielBetragText = value.ZielBetrag.ToString("F2", CultureInfo.CurrentCulture);
            AktuellerBetragText = value.AktuellerBetrag.ToString("F2", CultureInfo.CurrentCulture);
            MonatlicheSparrateText = value.MonatlicheSparrate?.ToString("F2", CultureInfo.CurrentCulture) ?? string.Empty;
            UseFaelligkeit = value.Faelligkeitsdatum.HasValue;
            Faelligkeitsdatum = value.Faelligkeitsdatum ?? DateTime.Today;
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(PageTitle));
            _ = LoadProgressAsync();
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationQueryKeys.SparZiel, out var val) && val is SparZiel sz)
            SparZiel = sz;
        else
            ResetForCreate();
    }

    public void ResetForCreate()
    {
        _sparZiel = null;
        Titel = string.Empty;
        Icon = "🎯";
        ZielBetragText = string.Empty;
        AktuellerBetragText = string.Empty;
        MonatlicheSparrateText = string.Empty;
        UseFaelligkeit = false;
        Faelligkeitsdatum = DateTime.Today;
        FortschrittProzent = 0;
        FortschrittText = string.Empty;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(PageTitle));
    }

    [RelayCommand]
    private async Task LoadProgressAsync()
    {
        if (_sparZiel == null) return;

        var summaries = await _loadSparZieleUseCase.ExecuteAsync();
        var summary = summaries.FirstOrDefault(s => s.SparZiel.Id == _sparZiel.Id);
        if (summary == null) return;

        FortschrittProzent = summary.FortschrittProzent;
        FortschrittText = string.Format(
            CultureInfo.CurrentCulture,
            _loc.GetString(ResourceKeys.Fmt_SparZielFortschritt),
            summary.GesamtFortschritt.ToString("C", CurrencyCulture.Instance),
            summary.SparZiel.ZielBetrag.ToString("C", CurrencyCulture.Instance));
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Titel))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_TitelErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        if (!TryParseAmount(ZielBetragText, out var zielBetrag) || zielBetrag <= 0)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_BetragGroesserNull),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        if (!TryParseAmount(AktuellerBetragText, out var aktuellerBetrag) || aktuellerBetrag < 0)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        decimal? monatlicheSparrate = null;
        if (!string.IsNullOrWhiteSpace(MonatlicheSparrateText))
        {
            if (!TryParseAmount(MonatlicheSparrateText, out var rate) || rate < 0)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Err_Titel),
                    _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            if (rate > 0)
                monatlicheSparrate = rate;
        }

        try
        {
            var sparZiel = _sparZiel ?? new SparZiel();
            sparZiel.Titel = Titel.Trim();
            sparZiel.Icon = string.IsNullOrWhiteSpace(Icon) ? "🎯" : Icon;
            sparZiel.ZielBetrag = zielBetrag;
            sparZiel.AktuellerBetrag = aktuellerBetrag;
            sparZiel.MonatlicheSparrate = monatlicheSparrate;
            sparZiel.Faelligkeitsdatum = UseFaelligkeit ? Faelligkeitsdatum : null;

            await _saveSparZielUseCase.ExecuteAsync(sparZiel);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
            if (IsEditing)
                await LoadProgressAsync();
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SparZielDetailViewModel: Save failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (_sparZiel == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_SparZielLoeschen),
            _loc.GetString(ResourceKeys.Dlg_SparZielLoeschenFrage, _sparZiel.Titel),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            await _deleteSparZielUseCase.ExecuteAsync(_sparZiel.Id);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SparZielDetailViewModel: Delete failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task BookContribution()
    {
        if (_sparZiel == null) return;

        await _navigationService.GoToAsync(Routes.TransactionDetail, new Dictionary<string, object>
        {
            [NavigationQueryKeys.SparZielContribution] = _sparZiel
        });
    }

    private static bool TryParseAmount(string text, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            amount = 0m;
            return true;
        }

        return FlexibleAmountParser.TryParse(text, out amount);
    }
}
