using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public enum TransactionSearchExecuteStatus
{
    Inactive,
    Stale,
    Success,
    Error
}

public sealed record TransactionSearchExecuteOutcome(
    TransactionSearchExecuteStatus Status,
    SearchTransactionsResult? Result = null);

/// <summary>
/// Debounced transaction search and filter picker data for the transactions list.
/// </summary>
public sealed class TransactionSearchCoordinator(
    SearchTransactionsUseCase searchTransactionsUseCase,
    LoadCategoriesUseCase loadCategoriesUseCase,
    LoadAccountsUseCase loadAccountsUseCase,
    IMainThreadDispatcher dispatcher,
    IDialogService dialogService,
    ILocalizationService localizationService,
    ILogger<TransactionSearchCoordinator> logger)
{
    private readonly SearchTransactionsUseCase _searchTransactionsUseCase = searchTransactionsUseCase;
    private readonly LoadCategoriesUseCase _loadCategoriesUseCase = loadCategoriesUseCase;
    private readonly LoadAccountsUseCase _loadAccountsUseCase = loadAccountsUseCase;
    private readonly IMainThreadDispatcher _dispatcher = dispatcher;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly ILogger<TransactionSearchCoordinator> _logger = logger;

    private CancellationTokenSource? _searchDebounce;
    private int _searchVersion;

    public void TriggerDebounced(Func<int, Task> executeSearchAsync)
    {
        var oldCts = _searchDebounce;
        _searchDebounce = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();
        var token = _searchDebounce.Token;
        var version = Interlocked.Increment(ref _searchVersion);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                    await _dispatcher.InvokeAsync(() => executeSearchAsync(version));
            }
            catch (TaskCanceledException) { }
        });
    }

    public void CancelPending() => _searchDebounce?.Cancel();

    public async Task<TransactionSearchExecuteOutcome> ExecuteSearchAsync(
        SearchTransactionsQuery query,
        bool isSearchActive,
        int version)
    {
        if (!isSearchActive)
            return new TransactionSearchExecuteOutcome(TransactionSearchExecuteStatus.Inactive);

        try
        {
            var result = await _searchTransactionsUseCase.ExecuteAsync(query);
            if (version >= 0 && version != _searchVersion)
                return new TransactionSearchExecuteOutcome(TransactionSearchExecuteStatus.Stale);
            return new TransactionSearchExecuteOutcome(TransactionSearchExecuteStatus.Success, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction search failed");
            if (version >= 0 && version != _searchVersion)
                return new TransactionSearchExecuteOutcome(TransactionSearchExecuteStatus.Stale);
            return new TransactionSearchExecuteOutcome(TransactionSearchExecuteStatus.Error);
        }
    }

    public async Task ShowSearchFailedAlertAsync()
    {
        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Err_Titel),
            _loc.GetString(ResourceKeys.Err_SucheFehlgeschlagen),
            _loc.GetString(ResourceKeys.Btn_OK));
    }

    public async Task<ObservableCollection<KategorieFilterItem>> LoadCategoryFilterItemsAsync()
    {
        var kategorien = await _loadCategoriesUseCase.ExecuteAsync();
        var items = new ObservableCollection<KategorieFilterItem>
        {
            new(null, _loc.GetString(ResourceKeys.Lbl_AlleKategorien), ResourceKeys.Lbl_AlleKategorien)
        };
        foreach (var k in kategorien.OrderBy(k => k.Name))
            items.Add(new KategorieFilterItem(k.Id, $"{k.Icon} {k.Name}"));
        return items;
    }

    public async Task<ObservableCollection<KategorieFilterItem>> LoadAccountFilterItemsAsync(string? selectedAccountId)
    {
        var konten = await _loadAccountsUseCase.ExecuteAsync();
        var items = new ObservableCollection<KategorieFilterItem>
        {
            new(null, _loc.GetString(ResourceKeys.Lbl_AlleKonten), ResourceKeys.Lbl_AlleKonten)
        };
        foreach (var konto in konten.OrderBy(k => k.Name))
            items.Add(new KategorieFilterItem(konto.Id, konto.Name));
        return items;
    }

    public KategorieFilterItem ResolveSelectedAccountItem(
        ObservableCollection<KategorieFilterItem> items,
        string? selectedAccountId) =>
        items.FirstOrDefault(i => i.Id == selectedAccountId) ?? items[0];
}
