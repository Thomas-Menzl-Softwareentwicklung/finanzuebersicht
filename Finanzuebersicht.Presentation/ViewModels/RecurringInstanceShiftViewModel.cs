using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Finanzuebersicht.ViewModels;

public partial class RecurringInstanceShiftViewModel(
    ShiftRecurringInstanceUseCase shiftRecurringInstanceUseCase,
    INavigationService navigationService,
    IDialogService dialogService,
    ILocalizationService localizationService,
    Finanzuebersicht.Core.Services.IClock? clock = null,
    ILogger<RecurringInstanceShiftViewModel>? logger = null) : ObservableObject, IApplyQueryAttributes
{
    private readonly ShiftRecurringInstanceUseCase _shiftRecurringInstanceUseCase = shiftRecurringInstanceUseCase;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly Finanzuebersicht.Core.Services.IClock _clock = clock ?? Finanzuebersicht.Core.Services.SystemClock.Instance;
    private readonly ILogger<RecurringInstanceShiftViewModel>? _logger = logger;

    [ObservableProperty]
    private string recurringId = string.Empty;

    [ObservableProperty]
    private DateTime instanceDate = Finanzuebersicht.Core.Services.SystemClock.Instance.Today;

    [ObservableProperty]
    private DateTime newDate = Finanzuebersicht.Core.Services.SystemClock.Instance.Today;

    [ObservableProperty]
    private string? note;

    partial void OnInstanceDateChanged(DateTime value)
    {
        NewDate = value;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationQueryKeys.RecurringId, out var id) && id is string recurringId)
            RecurringId = recurringId;
        if (query.TryGetValue(NavigationQueryKeys.InstanceDate, out var date) && date is DateTime instanceDate)
            InstanceDate = instanceDate;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrEmpty(RecurringId)) return;
        try
        {
            await _shiftRecurringInstanceUseCase.ExecuteAsync(RecurringId, InstanceDate.Date, NewDate.Date, Note);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RecurringInstanceShiftViewModel: {Context}", nameof(Save));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await _navigationService.GoBackAsync();
    }

}
