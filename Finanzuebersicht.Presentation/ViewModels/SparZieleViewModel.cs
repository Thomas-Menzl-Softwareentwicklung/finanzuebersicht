using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class SparZieleViewModel : ObservableObject, IAutoLoadViewModel, ICurrencyRefreshViewModel
{
    private readonly LoadSparZieleUseCase _loadUseCase;
    private readonly SaveSparZielUseCase _saveUseCase;
    private readonly DeleteSparZielUseCase _deleteUseCase;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _loc;
    private readonly IFeedbackService _feedbackService;
    private readonly IAppEvents _appEvents;
    private readonly ILogger<SparZieleViewModel>? _logger;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadSparZieleCommand;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private ObservableCollection<SparZielSummary> sparZiele = [];

    public bool IsEmpty => SparZiele.Count == 0;

    public bool IsEmptyStateVisible => IsEmpty && !ShowAddForm;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string neuerTitel = string.Empty;

    [ObservableProperty]
    private string neuerIcon = "🎯";

    [ObservableProperty]
    private decimal neuesZielBetrag;

    [ObservableProperty]
    private decimal neuerAktuellerBetrag;

    [ObservableProperty]
    private DateTime? neueFaelligkeit;

    [ObservableProperty]
    private decimal neueMonatlicheSparrate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private bool showAddForm;

    public SparZieleViewModel(
        LoadSparZieleUseCase loadUseCase,
        SaveSparZielUseCase saveUseCase,
        DeleteSparZielUseCase deleteUseCase,
        INavigationService navigationService,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IFeedbackService feedbackService,
        IAppEvents appEvents,
        ILogger<SparZieleViewModel>? logger = null)
    {
        _loadUseCase = loadUseCase;
        _saveUseCase = saveUseCase;
        _deleteUseCase = deleteUseCase;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _loc = localizationService;
        _feedbackService = feedbackService;
        _appEvents = appEvents;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadSparZiele()
    {
        CurrencyRefreshRegistry.Register(this);
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var items = await _loadUseCase.ExecuteAsync();
            SparZiele = new ObservableCollection<SparZielSummary>(items);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ToggleAddForm()
    {
        ShowAddForm = !ShowAddForm;
        if (ShowAddForm)
        {
            NeuerTitel = string.Empty;
            NeuerIcon = "🎯";
            NeuesZielBetrag = 0;
            NeuerAktuellerBetrag = 0;
            NeueFaelligkeit = null;
            NeueMonatlicheSparrate = 0;
        }
    }

    [RelayCommand]
    private async Task SaveNewSparZiel()
    {
        if (string.IsNullOrWhiteSpace(NeuerTitel))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_TitelErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }
        if (NeuesZielBetrag <= 0)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_BetragGroesserNull),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }
        if (NeuerAktuellerBetrag < 0)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            var ziel = new SparZiel
            {
                Titel = NeuerTitel,
                Icon = string.IsNullOrWhiteSpace(NeuerIcon) ? "🎯" : NeuerIcon,
                ZielBetrag = NeuesZielBetrag,
                AktuellerBetrag = NeuerAktuellerBetrag,
                Faelligkeitsdatum = NeueFaelligkeit,
                MonatlicheSparrate = NeueMonatlicheSparrate > 0 ? NeueMonatlicheSparrate : null
            };

            await _saveUseCase.ExecuteAsync(ziel);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SparZieleViewModel: {Context}", nameof(SaveNewSparZiel));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        ShowAddForm = false;
        await LoadSparZieleCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task UpdateBetrag(SparZiel ziel)
    {
        try
        {
            await _saveUseCase.ExecuteAsync(ziel);
            await LoadSparZieleCommand.ExecuteAsync(null);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SparZieleViewModel: {Context}", nameof(UpdateBetrag));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task OpenSparZiel(SparZielSummary summary)
    {
        await _navigationService.GoToAsync(Routes.SparZielDetail, new Dictionary<string, object>
        {
            ["SparZiel"] = summary.SparZiel
        });
    }

    [RelayCommand]
    private async Task DeleteSparZiel(string id)
    {
        var titel = SparZiele.FirstOrDefault(z => z.SparZiel.Id == id)?.SparZiel.Titel ?? id;
        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_SparZielLoeschen),
            _loc.GetString(ResourceKeys.Dlg_SparZielLoeschenFrage, titel),
            _loc.GetString(ResourceKeys.Btn_Ja), _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return;

        try
        {
            await _deleteUseCase.ExecuteAsync(id);
            await LoadSparZieleCommand.ExecuteAsync(null);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SparZieleViewModel: {Context}", nameof(DeleteSparZiel));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    public void RefreshCurrencyDisplay()
    {
        OnPropertyChanged(nameof(NeuesZielBetrag));
        OnPropertyChanged(nameof(NeuerAktuellerBetrag));
        OnPropertyChanged(nameof(NeueMonatlicheSparrate));
        if (SparZiele.Count > 0)
            SparZiele = CurrencyDisplayRefresh.Clone(SparZiele);
    }
}
