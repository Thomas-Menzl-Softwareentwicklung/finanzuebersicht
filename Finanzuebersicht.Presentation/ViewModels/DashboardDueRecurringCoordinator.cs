using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

/// <summary>
/// Due recurring load + book/skip/shift for the dashboard section.
/// </summary>
public sealed class DashboardDueRecurringCoordinator(
    GetDueRecurringWithHintsUseCase getDueRecurringUseCase,
    BookDueRecurringInstanceUseCase bookDueRecurringUseCase,
    SkipDueRecurringInstanceUseCase skipDueRecurringUseCase,
    IDialogService dialogService,
    INavigationService navigationService,
    ILocalizationService localizationService,
    IClock? clock = null,
    ILogger<DashboardDueRecurringCoordinator>? logger = null)
{
    private const int DashboardPreviewDays = 7;

    private readonly GetDueRecurringWithHintsUseCase _getDueRecurringUseCase = getDueRecurringUseCase;
    private readonly BookDueRecurringInstanceUseCase _bookDueRecurringUseCase = bookDueRecurringUseCase;
    private readonly SkipDueRecurringInstanceUseCase _skipDueRecurringUseCase = skipDueRecurringUseCase;
    private readonly IDialogService _dialogService = dialogService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IClock _clock = clock ?? SystemClock.Instance;
    private readonly ILogger<DashboardDueRecurringCoordinator>? _logger = logger;

    public async Task<ObservableCollection<DueRecurringItem>> LoadAsync()
    {
        var items = await _getDueRecurringUseCase.ExecuteAsync(_clock.Today);
        var actionable = items
            .Select(item =>
            {
                var hint = BuildHint(item);
                if (hint is null)
                    return null;

                item.Hint = hint;
                return item;
            })
            .Where(item => item is not null)
            .Cast<DueRecurringItem>()
            .ToList();

        return new ObservableCollection<DueRecurringItem>(actionable);
    }

    public async Task<bool> TryBookAsync(DueRecurringItem? item)
    {
        if (item == null) return false;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_DauerauftragBuchen),
            _loc.GetString(ResourceKeys.Dlg_DauerauftragBuchenFrage, item.Recurring.Titel, item.DueDate.ToString("d")),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return false;

        try
        {
            await _bookDueRecurringUseCase.ExecuteAsync(item.Recurring.Id, item.InstanceDate);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BookDueRecurring failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }
    }

    public async Task<bool> TrySkipAsync(DueRecurringItem? item)
    {
        if (item == null) return false;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_DauerauftragUeberspringen),
            _loc.GetString(ResourceKeys.Dlg_DauerauftragUeberspringenFrage, item.Recurring.Titel),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return false;

        try
        {
            await _skipDueRecurringUseCase.ExecuteAsync(item.Recurring.Id, item.InstanceDate);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SkipDueRecurring failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }
    }

    public Task ShiftAsync(DueRecurringItem? item)
    {
        if (item == null) return Task.CompletedTask;

        return _navigationService.GoToAsync(Routes.RecurringInstanceShift, new Dictionary<string, object>
        {
            [NavigationQueryKeys.RecurringId] = item.Recurring.Id,
            [NavigationQueryKeys.InstanceDate] = item.InstanceDate
        });
    }

    public Task NavigateToListAsync()
        => _navigationService.GoToAsync(Routes.RecurringTransactionsTab);

    private string? BuildHint(DueRecurringItem item)
    {
        var daysUntil = (item.DueDate.Date - _clock.Today.Date).Days;
        return daysUntil switch
        {
            0 => _loc.GetString(ResourceKeys.Hint_HeuteFaellig),
            < 0 => _loc.GetString(ResourceKeys.Hint_UeberfaelligSeitTagen, -daysUntil),
            > 0 when daysUntil <= DashboardPreviewDays => _loc.GetString(ResourceKeys.Hint_FaelligInTagen, daysUntil),
            > 0 when item.Recurring.ReminderDaysBefore > 0 && daysUntil <= item.Recurring.ReminderDaysBefore
                => _loc.GetString(ResourceKeys.Hint_FaelligInTagen, daysUntil),
            _ => null
        };
    }
}
