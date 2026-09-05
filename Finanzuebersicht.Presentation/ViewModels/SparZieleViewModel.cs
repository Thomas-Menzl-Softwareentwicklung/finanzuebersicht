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
    private readonly SparZielDetailViewModel _createSparZielViewModel;
    private readonly ISparZielCreateSheetService _sparZielCreateSheetService;
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

    public bool IsEmptyStateVisible => IsEmpty;

    [ObservableProperty]
    private bool isLoading;

    public SparZieleViewModel(
        LoadSparZieleUseCase loadUseCase,
        SaveSparZielUseCase saveUseCase,
        DeleteSparZielUseCase deleteUseCase,
        SparZielDetailViewModel createSparZielViewModel,
        ISparZielCreateSheetService sparZielCreateSheetService,
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
        _createSparZielViewModel = createSparZielViewModel;
        _sparZielCreateSheetService = sparZielCreateSheetService;
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
    private async Task OpenCreateForm()
    {
        _createSparZielViewModel.ResetForCreate();
        if (await _sparZielCreateSheetService.ShowAsync(_createSparZielViewModel))
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
            [NavigationQueryKeys.SparZiel] = summary.SparZiel
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

    public void RefreshCurrencyDisplay() => _ = LoadSparZieleCommand.ExecuteAsync(null);
}
