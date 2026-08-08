using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public sealed record DashboardAccountsSnapshot(
    ObservableCollection<AccountOverviewItem> Overview,
    decimal GesamtSaldo);

/// <summary>
/// Account filter + balances overview for the dashboard.
/// </summary>
public sealed class DashboardAccountsCoordinator(
    GetAccountBalancesUseCase getAccountBalancesUseCase,
    LoadActiveAccountsUseCase loadActiveAccountsUseCase,
    ILocalizationService localizationService)
{
    private readonly GetAccountBalancesUseCase _getAccountBalancesUseCase = getAccountBalancesUseCase;
    private readonly LoadActiveAccountsUseCase _loadActiveAccountsUseCase = loadActiveAccountsUseCase;
    private readonly ILocalizationService _loc = localizationService;

    public async Task EnsureFilterLoadedAsync(
        ObservableCollection<KategorieFilterItem> availableKonten,
        Action<ObservableCollection<KategorieFilterItem>> setAvailable,
        Action<KategorieFilterItem> setSelected)
    {
        if (availableKonten.Count > 0) return;

        var accounts = await _loadActiveAccountsUseCase.ExecuteAsync();
        var items = new ObservableCollection<KategorieFilterItem>
        {
            new(null, _loc.GetString(ResourceKeys.Lbl_AlleKonten), ResourceKeys.Lbl_AlleKonten)
        };

        foreach (var account in accounts)
            items.Add(new KategorieFilterItem(account.Id, account.Name));

        setAvailable(items);
        setSelected(items[0]);
    }

    public async Task<DashboardAccountsSnapshot> LoadOverviewAsync()
    {
        var balances = await _getAccountBalancesUseCase.ExecuteAsync();
        var active = balances.Where(b => !b.IsArchived).ToList();
        var gesamt = active.Sum(b => b.Saldo);
        var maxAbs = active.Count > 0 ? active.Max(b => Math.Abs(b.Saldo)) : 0m;
        var overview = new ObservableCollection<AccountOverviewItem>(
            active
                .OrderByDescending(b => Math.Abs(b.Saldo))
                .Select(b => new AccountOverviewItem
                {
                    AccountId = b.AccountId,
                    Name = b.AccountName,
                    Saldo = b.Saldo,
                    AnteilProzent = maxAbs > 0 ? Math.Abs(b.Saldo) / maxAbs * 100 : 0
                }));

        return new DashboardAccountsSnapshot(overview, gesamt);
    }

    public async Task<decimal> GetSelectedSaldoAsync(string? selectedAccountId)
    {
        if (string.IsNullOrWhiteSpace(selectedAccountId))
            return 0;

        var balances = await _getAccountBalancesUseCase.ExecuteAsync();
        return balances.FirstOrDefault(b => b.AccountId == selectedAccountId)?.Saldo ?? 0;
    }
}
