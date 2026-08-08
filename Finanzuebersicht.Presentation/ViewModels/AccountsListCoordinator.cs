using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public sealed record AccountsListSnapshot(
    ObservableCollection<AccountListItem> Items,
    decimal GesamtSaldoAktiv);

/// <summary>
/// Account list load/delete/archive/navigation for the Verwaltung tab.
/// </summary>
public sealed class AccountsListCoordinator(
    LoadAccountsUseCase loadAccountsUseCase,
    GetAccountBalancesUseCase getAccountBalancesUseCase,
    ToggleAccountArchiveUseCase toggleAccountArchiveUseCase,
    DeleteAccountUseCase deleteAccountUseCase,
    ILocalizationService localizationService,
    INavigationService navigationService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILicenseService? licenseService = null,
    ILogger<AccountsListCoordinator>? logger = null)
{
    private readonly LoadAccountsUseCase _loadAccountsUseCase = loadAccountsUseCase;
    private readonly GetAccountBalancesUseCase _getAccountBalancesUseCase = getAccountBalancesUseCase;
    private readonly ToggleAccountArchiveUseCase _toggleAccountArchiveUseCase = toggleAccountArchiveUseCase;
    private readonly DeleteAccountUseCase _deleteAccountUseCase = deleteAccountUseCase;
    private readonly ILocalizationService _loc = localizationService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILicenseService _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;
    private readonly ILogger<AccountsListCoordinator>? _logger = logger;

    public async Task<AccountsListSnapshot> LoadAsync()
    {
        var accounts = await _loadAccountsUseCase.ExecuteAsync();
        var balances = await _getAccountBalancesUseCase.ExecuteAsync();
        var balanceById = balances.ToDictionary(b => b.AccountId);
        var items = new ObservableCollection<AccountListItem>(
            accounts
                .OrderBy(a => a.IsArchived)
                .ThenBy(a => a.Name)
                .Select(a =>
                {
                    balanceById.TryGetValue(a.Id, out var summary);
                    return new AccountListItem(a, summary)
                    {
                        BalanceBreakdownText = summary is { OpeningBalance: not 0 }
                            ? _loc.GetString(
                                ResourceKeys.Fmt_KontoSaldoAufschluesselung,
                                summary.OpeningBalance.ToString("C", CurrencyCulture.Instance),
                                summary.TransactionBalance.ToString("C", CurrencyCulture.Instance))
                            : null
                    };
                }));

        return new AccountsListSnapshot(
            items,
            balances.Where(b => !b.IsArchived).Sum(b => b.Saldo));
    }

    public async Task<bool> TryDeleteAsync(AccountListItem konto, ObservableCollection<AccountListItem> konten)
    {
        if (!konto.CanDelete) return false;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_KontoLoeschen),
            _loc.GetString(ResourceKeys.Dlg_KontoLoeschenFrage, konto.Name),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return false;

        try
        {
            await _deleteAccountUseCase.ExecuteAsync(konto.Account.Id);
            konten.Remove(konto);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AccountsListCoordinator: TryDeleteAsync failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }
    }

    public async Task<bool> TryToggleArchiveAsync(AccountListItem konto)
    {
        if (!konto.CanArchive) return false;

        var setArchived = !konto.IsArchived;
        var confirmTitle = setArchived
            ? _loc.GetString(ResourceKeys.Dlg_KontoArchivieren)
            : _loc.GetString(ResourceKeys.Dlg_KontoReaktivieren);
        var confirmBody = setArchived
            ? _loc.GetString(ResourceKeys.Dlg_KontoArchivierenFrage, konto.Name)
            : _loc.GetString(ResourceKeys.Dlg_KontoReaktivierenFrage, konto.Name);
        var confirm = await _dialogService.ShowConfirmationAsync(
            confirmTitle,
            confirmBody,
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return false;

        try
        {
            await _toggleAccountArchiveUseCase.ExecuteAsync(konto.Account, setArchived);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AccountsListCoordinator: TryToggleArchiveAsync failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }
    }

    public Task NavigateToDetailAsync(AccountListItem konto)
        => _navigationService.GoToAsync(Routes.AccountDetail, new Dictionary<string, object>
        {
            [NavigationQueryKeys.AccountId] = konto.Account.Id
        });

    public async Task NavigateToCreateAsync(int currentAccountCount)
    {
        var check = _licenseService.CheckCreateLimit(LimitedResource.Accounts, currentAccountCount);
        if (!check.Allowed)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LimitErreicht, check.CurrentCount, check.Limit!.Value),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        await _navigationService.GoToAsync(Routes.AccountDetail);
    }
}
