using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public partial class LicenseViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;
    private readonly ILicenseEntitlementStore _entitlementStore;
    private readonly IStoreBillingService _billingService;
    private readonly ILocalizationService _loc;
    private readonly IDialogService _dialogService;
    private readonly IFeedbackService _feedbackService;

    public LicenseViewModel(
        ILicenseService licenseService,
        ILicenseEntitlementStore entitlementStore,
        IStoreBillingService billingService,
        ILocalizationService localizationService,
        IDialogService dialogService,
        IFeedbackService feedbackService)
    {
        _licenseService = licenseService;
        _entitlementStore = entitlementStore;
        _billingService = billingService;
        _loc = localizationService;
        _dialogService = dialogService;
        _feedbackService = feedbackService;
        RefreshFromService();
    }

    [ObservableProperty]
    private string channelLabel = string.Empty;

    [ObservableProperty]
    private string tierLabel = string.Empty;

    [ObservableProperty]
    private string syncLabel = string.Empty;

    [ObservableProperty]
    private string limitsHint = string.Empty;

    [ObservableProperty]
    private string proPriceLabel = string.Empty;

    [ObservableProperty]
    private bool showStorePurchaseControls;

    [ObservableProperty]
    private bool showStoreStubControls;

    [ObservableProperty]
    private bool canBuyPro;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool stubProEnabled;

    [ObservableProperty]
    private bool stubSyncEnabled;

    public async Task InitializeAsync()
    {
        await _licenseService.RefreshAsync();
        await LoadProductPriceAsync();
        RefreshFromService();
    }

    [RelayCommand]
    private async Task BuyPro()
    {
        if (IsBusy || !_billingService.IsAvailable || _licenseService.HasPro)
            return;

        IsBusy = true;
        try
        {
            var result = await _billingService.PurchaseAsync(LicenseProductIds.Pro);
            if (result.WasCancelled)
                return;

            if (!result.IsSuccess)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Err_Titel),
                    result.ErrorMessage ?? _loc.GetString(ResourceKeys.Lic_PurchaseFailed),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            await PersistOwnedAndRefreshAsync();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Lic_PurchaseSuccess));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestorePurchases()
    {
        if (IsBusy || !_billingService.IsAvailable)
            return;

        IsBusy = true;
        try
        {
            await _entitlementStore.ClearStubPreferenceAsync();
            var ok = await _billingService.RestorePurchasesAsync();
            await PersistOwnedAndRefreshAsync();
            await _feedbackService.ShowSnackbarAsync(ok
                ? _loc.GetString(ResourceKeys.Lic_RestoreSuccess)
                : _loc.GetString(ResourceKeys.Lic_RestoreEmpty));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyStubEntitlements()
    {
        if (_licenseService.Channel != DistributionChannel.Store)
            return;

        await _entitlementStore.SetStubEntitlementsAsync(StubProEnabled, StubSyncEnabled);
        await _licenseService.RefreshAsync();
        RefreshFromService();
    }

    [RelayCommand]
    private async Task UseStoreKitInsteadOfStub()
    {
        await _entitlementStore.ClearStubPreferenceAsync();
        await _licenseService.RefreshAsync();
        await LoadProductPriceAsync();
        RefreshFromService();
    }

    private async Task PersistOwnedAndRefreshAsync()
    {
        var owned = await _billingService.GetOwnedProductIdsAsync();
        await _entitlementStore.ApplyOwnedProductIdsAsync(owned);
        await _licenseService.RefreshAsync();
        RefreshFromService();
    }

    private async Task LoadProductPriceAsync()
    {
        ProPriceLabel = string.Empty;
        if (!_billingService.IsAvailable)
            return;

        try
        {
            if (!await _billingService.InitializeAsync())
                return;

            var products = await _billingService.GetProductsAsync();
            var pro = products.FirstOrDefault(p => p.Id == LicenseProductIds.Pro);
            if (pro != null && !string.IsNullOrWhiteSpace(pro.LocalizedPrice))
                ProPriceLabel = pro.LocalizedPrice;
        }
        catch
        {
            // Sandbox / missing products — UI still shows Buy without price.
        }
    }

    private void RefreshFromService()
    {
        var isStore = _licenseService.Channel == DistributionChannel.Store;

        ChannelLabel = isStore
            ? _loc.GetString(ResourceKeys.Lic_ChannelStore)
            : _loc.GetString(ResourceKeys.Lic_ChannelDirect);

        TierLabel = _licenseService.HasPro
            ? _loc.GetString(ResourceKeys.Lic_TierPro)
            : _loc.GetString(ResourceKeys.Lic_TierFree);

        ShowStorePurchaseControls = isStore && _billingService.IsAvailable;
        CanBuyPro = ShowStorePurchaseControls && !_licenseService.HasPro;
        // Stub toggles only on Store builds (useful before ASC products exist / on simulator)
        ShowStoreStubControls = isStore;

        if (!isStore)
        {
            SyncLabel = _loc.GetString(ResourceKeys.Lic_SyncUnavailableDirect);
            LimitsHint = _loc.GetString(ResourceKeys.Lic_DirectFullLocal);
        }
        else if (_licenseService.CanUseCloudSync)
        {
            SyncLabel = _licenseService.IsCloudSyncImplemented
                ? _loc.GetString(ResourceKeys.Lic_SyncActive)
                : _loc.GetString(ResourceKeys.Lic_SyncEntitledComingSoon);
            LimitsHint = _licenseService.HasPro
                ? string.Empty
                : _loc.GetString(ResourceKeys.Lic_FreeLimitsHint);
        }
        else
        {
            SyncLabel = _loc.GetString(ResourceKeys.Lic_SyncComingSoon);
            LimitsHint = _licenseService.HasPro
                ? string.Empty
                : _loc.GetString(ResourceKeys.Lic_FreeLimitsHint);
        }

        StubProEnabled = _licenseService.HasPro && isStore;
        StubSyncEnabled = _licenseService.HasSyncSubscription;
    }
}
