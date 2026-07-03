using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public partial class RecurringTransactionDetailViewModel(
    SaveRecurringTransactionDetailUseCase saveRecurringTransactionDetailUseCase,
    LoadRecurringTransactionDetailDataUseCase loadRecurringTransactionDetailDataUseCase,
    AddRecurringExceptionUseCase addRecurringExceptionUseCase,
    RemoveRecurringExceptionUseCase removeRecurringExceptionUseCase,
    ITransactionValidationService validationService,
    INavigationService navigationService,
    IDialogService dialogService,
    ILocalizationService localizationService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<RecurringTransactionDetailViewModel>? logger = null,
    Finanzuebersicht.Core.Services.IClock? clock = null) : ObservableObject, IAutoLoadViewModel, IApplyQueryAttributes, ILocalizableViewModel
{
    private readonly SaveRecurringTransactionDetailUseCase _saveRecurringTransactionDetailUseCase = saveRecurringTransactionDetailUseCase;
    private readonly LoadRecurringTransactionDetailDataUseCase _loadRecurringTransactionDetailDataUseCase = loadRecurringTransactionDetailDataUseCase;
    private readonly AddRecurringExceptionUseCase _addRecurringExceptionUseCase = addRecurringExceptionUseCase;
    private readonly RemoveRecurringExceptionUseCase _removeRecurringExceptionUseCase = removeRecurringExceptionUseCase;
    private readonly ITransactionValidationService _validationService = validationService;
    private RecurringTransaction? _existing;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<RecurringTransactionDetailViewModel>? _logger = logger;
    private readonly Finanzuebersicht.Core.Services.IClock _clock = clock ?? Finanzuebersicht.Core.Services.SystemClock.Instance;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadKategorienCommand;

    [ObservableProperty]
    private string betragText = string.Empty;

    [ObservableProperty]
    private string titel = string.Empty;

    [ObservableProperty]
    private Category? selectedKategorie;

    [ObservableProperty]
    private Account? selectedAccount;

    [ObservableProperty]
    private TransactionType typ = TransactionType.Ausgabe;

    [ObservableProperty]
    private DateTime startdatum = Finanzuebersicht.Core.Services.SystemClock.Instance.Today;

    [ObservableProperty]
    private DateTime? enddatum;

    [ObservableProperty]
    private bool hatEnddatum;

    [ObservableProperty]
    private DateTime enddatumWert = Finanzuebersicht.Core.Services.SystemClock.Instance.Today.AddYears(1);

    [ObservableProperty]
    private bool aktiv = true;

    [ObservableProperty]
    private ObservableCollection<Category> kategorien = [];

    [ObservableProperty]
    private ObservableCollection<Account> accounts = [];

    [ObservableProperty]
    private RecurrenceInterval interval = RecurrenceInterval.Monthly;

    [ObservableProperty]
    private RecurrenceIntervalOption? selectedIntervalOption;

    // string-backed entries to avoid binding conversion errors; numeric backing kept for logic
    [ObservableProperty]
    private string intervalFactorText = "1";

    [ObservableProperty]
    private int intervalFactor = 1;

    [ObservableProperty]
    private string reminderDaysBeforeText = "0";

    [ObservableProperty]
    private int reminderDaysBefore = 0;

    [ObservableProperty]
    private ObservableCollection<RecurringException> exceptions = [];

    private List<RecurrenceIntervalOption>? _verfuegbareIntervalle;

    public IReadOnlyList<RecurrenceIntervalOption> VerfuegbareIntervalle =>
        _verfuegbareIntervalle ??= BuildIntervalOptions();

    public string PageTitle => _loc.GetString(ResourceKeys.Title_Dauerauftrag);

    public bool IsEditing => _existing != null;

    partial void OnIntervalChanged(RecurrenceInterval value)
    {
        SelectedIntervalOption = VerfuegbareIntervalle.FirstOrDefault(option => option.Value == value);
    }

    partial void OnSelectedIntervalOptionChanged(RecurrenceIntervalOption? value)
    {
        if (value != null && Interval != value.Value)
            Interval = value.Value;
    }

    public RecurringTransaction? RecurringTransaction
    {
        set
        {
            if (value != null)
            {
                _existing = value;
                BetragText = value.Betrag.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
                Titel = value.Titel;
                Typ = value.Typ;
                Startdatum = value.Startdatum;
                Aktiv = value.Aktiv;

                if (value.Enddatum.HasValue)
                {
                    HatEnddatum = true;
                    EnddatumWert = value.Enddatum.Value;
                }

                _ = SetKategorieAsync(value.KategorieId);
                _ = SetAccountAsync(value.AccountId);
                Interval = value.Interval;
                IntervalFactor = value.IntervalFactor;
                IntervalFactorText = value.IntervalFactor.ToString();
                ReminderDaysBefore = value.ReminderDaysBefore;
                ReminderDaysBeforeText = value.ReminderDaysBefore.ToString();
                Exceptions = new ObservableCollection<RecurringException>(value.Exceptions ?? new List<RecurringException>());
                OnPropertyChanged(nameof(IsEditing));
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("RecurringTransaction", out var val) && val is RecurringTransaction rt)
            RecurringTransaction = rt;
    }

    [RelayCommand]
    private async Task LoadKategorien()
    {
        var currentCategoryId = SelectedKategorie?.Id ?? _existing?.KategorieId;
        var currentAccountId = SelectedAccount?.Id ?? _existing?.AccountId;
        var data = await _loadRecurringTransactionDetailDataUseCase.ExecuteAsync(currentCategoryId, currentAccountId);
        Kategorien = new ObservableCollection<Category>(data.Kategorien);
        Accounts = new ObservableCollection<Account>(data.Accounts);
        SelectedKategorie = data.SelectedKategorie;
        SelectedAccount = data.SelectedAccount;
        SelectedIntervalOption ??= VerfuegbareIntervalle.FirstOrDefault(option => option.Value == Interval);
    }

    [RelayCommand]
    private void SetTyp(string typName)
    {
        Typ = typName == "Einnahme" ? TransactionType.Einnahme : TransactionType.Ausgabe;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (await TrySaveAsync())
            await _navigationService.GoBackAsync();
    }

    public async Task ResetForCreateAsync()
    {
        _existing = null;
        BetragText = string.Empty;
        Titel = string.Empty;
        Typ = TransactionType.Ausgabe;
        Startdatum = _clock.Today;
        HatEnddatum = false;
        EnddatumWert = _clock.Today.AddYears(1);
        Aktiv = true;
        Interval = RecurrenceInterval.Monthly;
        IntervalFactor = 1;
        IntervalFactorText = "1";
        ReminderDaysBefore = 0;
        ReminderDaysBeforeText = "0";
        Exceptions = [];
        SelectedIntervalOption = VerfuegbareIntervalle.FirstOrDefault(option => option.Value == Interval);
        OnPropertyChanged(nameof(IsEditing));
        await LoadKategorienCommand.ExecuteAsync(null);
    }

    public async Task<bool> TrySaveAsync()
    {
        if (!_validationService.TryValidate(
                BetragText,
                Titel,
                SelectedKategorie != null,
                System.Globalization.CultureInfo.CurrentCulture,
                out var betrag,
                out var error))
        {
            var message = error switch
            {
                TransactionInputError.InvalidAmountFormat => _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                TransactionInputError.AmountMustBePositive => _loc.GetString(ResourceKeys.Err_BetragGroesserNull),
                TransactionInputError.TitleRequired => _loc.GetString(ResourceKeys.Err_TitelErforderlich),
                TransactionInputError.CategoryRequired => _loc.GetString(ResourceKeys.Err_KategorieErforderlich),
                _ => _loc.GetString(ResourceKeys.Err_UngueltigerBetrag)
            };
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                message,
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }

        if (!int.TryParse(IntervalFactorText, out var parsedFactor) || parsedFactor <= 0)
        {
            parsedFactor = 1;
            IntervalFactorText = "1";
        }

        if (!int.TryParse(ReminderDaysBeforeText, out var parsedReminder) || parsedReminder < 0)
        {
            parsedReminder = 0;
            ReminderDaysBeforeText = "0";
        }

        IntervalFactor = parsedFactor;
        ReminderDaysBefore = parsedReminder;

        try
        {
            await _saveRecurringTransactionDetailUseCase.ExecuteAsync(
                _existing,
                betrag,
                Titel,
                SelectedKategorie!.Id,
                SelectedAccount?.Id,
                Typ,
                Startdatum,
                HatEnddatum ? EnddatumWert : null,
                Aktiv,
                Interval,
                IntervalFactor,
                ReminderDaysBefore,
                Exceptions.ToList());
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RecurringTransactionDetailViewModel: {Context}", nameof(TrySaveAsync));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }
    }

    [RelayCommand]
    private async Task AddSkipNextInstance()
    {
        if (_existing == null) return;

        DateTime next;
        if (_existing.LetzteAusfuehrung.HasValue)
        {
            next = RecurringScheduleCalculator.GetNextInstance(_existing, _existing.LetzteAusfuehrung.Value);
        }
        else
        {
            // if no last execution, the next instance is the start date itself
            next = _existing.Startdatum.Date;
        }

        var ex = new RecurringException { Id = Guid.NewGuid().ToString(), InstanceDate = next.Date, Type = RecurringExceptionType.Skip };
        await _addRecurringExceptionUseCase.ExecuteAsync(_existing.Id, ex);
        Exceptions.Add(ex);
    }

    [RelayCommand]
    private async Task GoToShift()
    {
        if (_existing == null) return;

        DateTime next;
        if (_existing.LetzteAusfuehrung.HasValue)
        {
            next = RecurringScheduleCalculator.GetNextInstance(_existing, _existing.LetzteAusfuehrung.Value);
        }
        else
        {
            next = _existing.Startdatum.Date;
        }

        var parameters = new Dictionary<string, object>
        {
            ["RecurringId"] = _existing.Id,
            ["InstanceDate"] = next.Date
        };

        await _navigationService.GoToAsync(Routes.RecurringInstanceShift, parameters);
    }

    [RelayCommand]
    private async Task RemoveException(RecurringException ex)
    {
        if (_existing == null || ex == null) return;
        await _removeRecurringExceptionUseCase.ExecuteAsync(_existing.Id, ex.Id);
        Exceptions.Remove(ex);
    }

    private async Task SetKategorieAsync(string kategorieId)
    {
        try
        {
            if (Kategorien.Count == 0)
                await LoadKategorien();

            SelectedKategorie = Kategorien.FirstOrDefault(k => k.Id == kategorieId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fehler beim Laden der Kategorie");
        }
    }

    private async Task SetAccountAsync(string? accountId)
    {
        try
        {
            if (Accounts.Count == 0)
                await LoadKategorien();

            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == accountId)
                ?? Accounts.FirstOrDefault(a => a.SystemKey == Finanzuebersicht.Constants.SystemAccountKeys.Default)
                ?? Accounts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fehler beim Laden des Kontos");
        }
    }

    private List<RecurrenceIntervalOption> BuildIntervalOptions() =>
    [
        new(RecurrenceInterval.Daily, _loc.GetString(EnumResourceKeys.GetRecurrenceInterval(RecurrenceInterval.Daily))),
        new(RecurrenceInterval.Weekly, _loc.GetString(EnumResourceKeys.GetRecurrenceInterval(RecurrenceInterval.Weekly))),
        new(RecurrenceInterval.Monthly, _loc.GetString(EnumResourceKeys.GetRecurrenceInterval(RecurrenceInterval.Monthly))),
        new(RecurrenceInterval.Quarterly, _loc.GetString(EnumResourceKeys.GetRecurrenceInterval(RecurrenceInterval.Quarterly))),
        new(RecurrenceInterval.Yearly, _loc.GetString(EnumResourceKeys.GetRecurrenceInterval(RecurrenceInterval.Yearly)))
    ];

    public void RefreshLocalizedStrings()
    {
        _verfuegbareIntervalle = null;
        OnPropertyChanged(nameof(VerfuegbareIntervalle));
        OnPropertyChanged(nameof(PageTitle));
        SelectedIntervalOption = VerfuegbareIntervalle.FirstOrDefault(option => option.Value == Interval);
    }
}

public sealed record RecurrenceIntervalOption(RecurrenceInterval Value, string DisplayName);
