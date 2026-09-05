using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class CategoriesViewModel(
    CategoriesListCoordinator categoriesCoordinator,
    AccountsListCoordinator accountsCoordinator,
    ILocalizationService localizationService,
    IDialogService dialogService,
    ILogger<CategoriesViewModel>? logger = null) : ObservableObject, IAutoLoadViewModel, ILocalizableViewModel, ICurrencyRefreshViewModel
{
    private readonly CategoriesListCoordinator _categoriesCoordinator = categoriesCoordinator;
    private readonly AccountsListCoordinator _accountsCoordinator = accountsCoordinator;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILogger<CategoriesViewModel>? _logger = logger;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadKategorienCommand;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKategorienVisible))]
    [NotifyPropertyChangedFor(nameof(IsKontenVisible))]
    [NotifyPropertyChangedFor(nameof(IsKategorienEmpty))]
    private ObservableCollection<Category> kategorien = [];

    public bool IsKategorienEmpty => Kategorien.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGesamtSaldoHeader))]
    [NotifyPropertyChangedFor(nameof(IsKontenEmpty))]
    [NotifyPropertyChangedFor(nameof(IsKontenEmptyStateVisible))]
    private ObservableCollection<AccountListItem> konten = [];

    public bool IsKontenEmpty => Konten.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGesamtSaldoHeader))]
    private decimal gesamtSaldoAktiv;

    public bool ShowGesamtSaldoHeader => Konten.Any(k => !k.IsArchived);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKategorienVisible))]
    [NotifyPropertyChangedFor(nameof(IsKontenVisible))]
    private int selectedSectionIndex;

    public bool IsKategorienVisible => SelectedSectionIndex == 0;
    public bool IsKontenVisible => SelectedSectionIndex == 1;

    public string FabAccessibilityDescription => IsKontenVisible
        ? _loc.GetString(ResourceKeys.A11y_KontoHinzufuegen)
        : _loc.GetString(ResourceKeys.A11y_KategorieHinzufuegen);

    partial void OnSelectedSectionIndexChanged(int value)
    {
        OnPropertyChanged(nameof(FabAccessibilityDescription));
    }

    public void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(FabAccessibilityDescription));
        _ = LoadKategorienCore();
    }

    public void RefreshCurrencyDisplay() => _ = LoadKategorienCore(force: true);

    [ObservableProperty]
    private bool isLoading;

    public bool IsKontenEmptyStateVisible => IsKontenEmpty;

    [RelayCommand]
    private void ShowKategorien() => SelectedSectionIndex = 0;

    [RelayCommand]
    private void ShowKonten() => SelectedSectionIndex = 1;

    [RelayCommand]
    private Task LoadKategorien() => LoadKategorienCore();

    private async Task LoadKategorienCore(bool force = false)
    {
        CurrencyRefreshRegistry.Register(this);
        if (!force && IsLoading) return;
        IsLoading = true;

        try
        {
            Kategorien = await _categoriesCoordinator.LoadAsync();
            var accounts = await _accountsCoordinator.LoadAsync();
            Konten = accounts.Items;
            GesamtSaldoAktiv = accounts.GesamtSaldoAktiv;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoriesViewModel: {Context}", nameof(LoadKategorien));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LadenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteKategorie(Category kategorie)
    {
        if (await _categoriesCoordinator.TryDeleteAsync(kategorie, Kategorien))
            OnPropertyChanged(nameof(IsKategorienEmpty));
    }

    [RelayCommand]
    private async Task DeleteKonto(AccountListItem konto)
    {
        if (!await _accountsCoordinator.TryDeleteAsync(konto, Konten))
            return;

        OnPropertyChanged(nameof(IsKontenEmpty));
        OnPropertyChanged(nameof(IsKontenEmptyStateVisible));
        OnPropertyChanged(nameof(ShowGesamtSaldoHeader));
    }

    [RelayCommand]
    private async Task ToggleKontoArchivierung(AccountListItem konto)
    {
        if (!await _accountsCoordinator.TryToggleArchiveAsync(konto))
            return;

        await LoadKategorien();
    }

    [RelayCommand]
    private async Task GoToDetail(object? item = null)
    {
        if (item is AccountListItem kontoItem)
        {
            await _accountsCoordinator.NavigateToDetailAsync(kontoItem);
            return;
        }

        if (item is Category kategorie)
        {
            await _categoriesCoordinator.NavigateToDetailAsync(kategorie);
            return;
        }

        if (item == null && IsKontenVisible)
        {
            if (await _accountsCoordinator.NavigateToCreateAsync(Konten.Count))
                await LoadKategorien();
            return;
        }

        if (await _categoriesCoordinator.NavigateToCreateAsync())
            await LoadKategorien();
    }
}
