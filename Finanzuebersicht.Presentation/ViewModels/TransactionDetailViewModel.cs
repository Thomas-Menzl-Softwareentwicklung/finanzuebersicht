using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public partial class TransactionDetailViewModel(
    SaveTransactionDetailUseCase saveTransactionDetailUseCase,
    LoadTransactionDetailDataUseCase loadTransactionDetailDataUseCase,
    LoadSparZieleUseCase loadSparZieleUseCase,
    ITransactionValidationService validationService,
    ILocalizationService localizationService,
    INavigationService navigationService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<TransactionDetailViewModel>? logger = null,
    Finanzuebersicht.Core.Services.IClock? clock = null,
    SaveTransactionTemplateUseCase? saveTransactionTemplateUseCase = null) : ObservableObject, IAutoLoadViewModel, IApplyQueryAttributes
{
    private readonly SaveTransactionDetailUseCase _saveTransactionDetailUseCase = saveTransactionDetailUseCase;
    private readonly LoadTransactionDetailDataUseCase _loadTransactionDetailDataUseCase = loadTransactionDetailDataUseCase;
    private readonly LoadSparZieleUseCase _loadSparZieleUseCase = loadSparZieleUseCase;
    private readonly ITransactionValidationService _validationService = validationService;
    private Transaction? _existingTransaction;
    private string? _selectedKategorieId;
    private string? _selectedAccountId;
    private string? _selectedSparZielId;
    private readonly ILocalizationService _loc = localizationService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<TransactionDetailViewModel>? _logger = logger;
    private readonly Finanzuebersicht.Core.Services.IClock _clock = clock ?? Finanzuebersicht.Core.Services.SystemClock.Instance;
    private readonly SaveTransactionTemplateUseCase? _saveTransactionTemplateUseCase = saveTransactionTemplateUseCase;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadKategorienCommand;

    [ObservableProperty]
    private string betragText = string.Empty;

    [ObservableProperty]
    private string titel = string.Empty;

    [ObservableProperty]
    private string verwendungszweck = string.Empty;

    [ObservableProperty]
    private DateTime datum = Finanzuebersicht.Core.Services.SystemClock.Instance.Today;

    [ObservableProperty]
    private Category? selectedKategorie;

    [ObservableProperty]
    private Account? selectedAccount;

    [ObservableProperty]
    private TransactionType typ = TransactionType.Ausgabe;

    [ObservableProperty]
    private ObservableCollection<Category> kategorien = [];

    [ObservableProperty]
    private ObservableCollection<Account> accounts = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSparZiele))]
    private ObservableCollection<SparZiel> sparZiele = [];

    public bool HasSparZiele => SparZiele.Count > 0;

    [ObservableProperty]
    private SparZiel? selectedSparZiel;

    public Transaction? Transaction
    {
        set
        {
            if (value != null)
            {
                _existingTransaction = value;
                _selectedKategorieId = value.KategorieId;
                _selectedAccountId = value.AccountId;
                _selectedSparZielId = value.SparZielId;
                BetragText = value.Betrag.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
                Titel = value.Titel;
                Verwendungszweck = value.Verwendungszweck ?? string.Empty;
                Datum = value.Datum;
                Typ = value.Typ;

                // Kategorie nach Laden setzen
                _ = SetKategorieAsync(value.KategorieId);
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationQueryKeys.Transaction, out var val) && val is Transaction t)
        {
            Transaction = t;
        }
        else if (query.TryGetValue(NavigationQueryKeys.DuplicateTransaction, out var duplicateVal) && duplicateVal is Transaction duplicate)
        {
            ApplyTransactionDraft(duplicate, _clock.Today);
        }
        else if (query.TryGetValue(NavigationQueryKeys.TransactionTemplate, out var templateVal) && templateVal is TransactionTemplate template)
        {
            ApplyTemplateDraft(template);
        }
        else if (query.TryGetValue(NavigationQueryKeys.SparZielContribution, out var contributionVal) && contributionVal is SparZiel sparZiel)
        {
            ApplySparZielContribution(sparZiel);
        }
    }

    [RelayCommand]
    private async Task LoadKategorien()
    {
        var data = await _loadTransactionDetailDataUseCase.ExecuteAsync(
            _selectedKategorieId ?? _existingTransaction?.KategorieId,
            _selectedAccountId ?? _existingTransaction?.AccountId);
        Kategorien = new ObservableCollection<Category>(data.Kategorien);
        SelectedKategorie = data.SelectedKategorie;
        Accounts = new ObservableCollection<Account>(data.Accounts);
        SelectedAccount = data.SelectedAccount;

        var summaries = await _loadSparZieleUseCase.ExecuteAsync();
        SparZiele = new ObservableCollection<SparZiel>(summaries.Select(s => s.SparZiel));
        SelectedSparZiel = SparZiele.FirstOrDefault(z => z.Id == _selectedSparZielId);
    }

    [RelayCommand]
    private void SetTyp(string typName)
    {
        Typ = typName == "Einnahme" ? TransactionType.Einnahme : TransactionType.Ausgabe;
    }

    [RelayCommand]
    private async Task Save()
    {
        try
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
                return;
            }

            await _saveTransactionDetailUseCase.ExecuteAsync(
                _existingTransaction,
                betrag,
                Titel,
                Datum,
                SelectedKategorie!.Id,
                _selectedAccountId ?? SelectedAccount?.Id,
                Typ,
                Verwendungszweck,
                SelectedSparZiel?.Id);
            _appEvents.NotifyDataChanged();
            await _navigationService.GoBackAsync();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving transaction");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task SaveAsTemplate()
    {
        if (_saveTransactionTemplateUseCase == null) return;

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
            return;
        }

        var template = new TransactionTemplate
        {
            Name = Titel,
            Titel = Titel,
            Betrag = betrag,
            KategorieId = SelectedKategorie!.Id,
            AccountId = _selectedAccountId ?? SelectedAccount?.Id,
            Typ = Typ,
            Verwendungszweck = Verwendungszweck ?? string.Empty
        };

        await _saveTransactionTemplateUseCase.ExecuteAsync(template);
        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Msg_VorlageGespeichert_Title),
            _loc.GetString(ResourceKeys.Msg_VorlageGespeichert_Body),
            _loc.GetString(ResourceKeys.Btn_OK));
    }

    private async Task SetKategorieAsync(string kategorieId)
    {
        _selectedKategorieId = kategorieId;

        try
        {
            if (Kategorien.Count == 0)
            {
                await LoadKategorien();
            }

            SelectedKategorie = Kategorien.FirstOrDefault(k => k.Id == kategorieId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fehler beim Laden der Kategorie");
        }
    }

    private async Task SetAccountAsync(string? accountId)
    {
        _selectedAccountId = accountId;

        try
        {
            if (Accounts.Count == 0)
            {
                await LoadKategorien();
                return;
            }

            SelectedAccount = accountId == null
                ? Accounts.FirstOrDefault(a => a.SystemKey == Finanzuebersicht.Constants.SystemAccountKeys.Default)
                    ?? Accounts.FirstOrDefault()
                : Accounts.FirstOrDefault(a => a.Id == accountId)
                    ?? Accounts.FirstOrDefault(a => a.SystemKey == Finanzuebersicht.Constants.SystemAccountKeys.Default)
                    ?? Accounts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fehler beim Laden des Kontos");
        }
    }

    private void ApplyTransactionDraft(Transaction source, DateTime datum)
    {
        _existingTransaction = null;
        BetragText = source.Betrag.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
        Titel = source.Titel;
        Verwendungszweck = source.Verwendungszweck ?? string.Empty;
        Datum = datum;
        Typ = source.Typ;
        _selectedKategorieId = source.KategorieId;
        _selectedAccountId = source.AccountId;
        _ = SetKategorieAsync(source.KategorieId);
        _ = SetAccountAsync(source.AccountId);
    }

    private void ApplySparZielContribution(SparZiel sparZiel)
    {
        _existingTransaction = null;
        _selectedSparZielId = sparZiel.Id;
        Titel = sparZiel.Titel;
        Typ = TransactionType.Einnahme;
        Datum = _clock.Today;
        if (sparZiel.MonatlicheSparrate is > 0)
            BetragText = sparZiel.MonatlicheSparrate.Value.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
    }

    private void ApplyTemplateDraft(TransactionTemplate template)
    {
        _existingTransaction = null;
        BetragText = template.Betrag.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
        Titel = template.Titel;
        Verwendungszweck = template.Verwendungszweck ?? string.Empty;
        Datum = _clock.Today;
        Typ = template.Typ;
        _selectedKategorieId = template.KategorieId;
        _selectedAccountId = template.AccountId;
        _ = SetKategorieAsync(template.KategorieId);
        _ = SetAccountAsync(template.AccountId);
    }
}
