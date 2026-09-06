using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Sync;
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
    private readonly EnableCloudSyncUseCase _enableCloudSyncUseCase;
    private readonly ISyncMetadataStore _syncMetadataStore;
    private readonly ICloudSyncOrchestrator _cloudSyncOrchestrator;
    private bool _suppressCloudSyncToggle;
    private bool _lastKnownCloudSyncEnabled;
    private bool _metadataSyncEnabled;

    public LicenseViewModel(
        ILicenseService licenseService,
        ILicenseEntitlementStore entitlementStore,
        IStoreBillingService billingService,
        ILocalizationService localizationService,
        IDialogService dialogService,
        IFeedbackService feedbackService,
        EnableCloudSyncUseCase enableCloudSyncUseCase,
        ISyncMetadataStore syncMetadataStore,
        ICloudSyncOrchestrator cloudSyncOrchestrator)
    {
        _licenseService = licenseService;
        _entitlementStore = entitlementStore;
        _billingService = billingService;
        _loc = localizationService;
        _dialogService = dialogService;
        _feedbackService = feedbackService;
        _enableCloudSyncUseCase = enableCloudSyncUseCase;
        _syncMetadataStore = syncMetadataStore;
        _cloudSyncOrchestrator = cloudSyncOrchestrator;
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
    private string syncPriceLabel = string.Empty;

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

    [ObservableProperty]
    private bool cloudSyncEnabled;

    [ObservableProperty]
    private string cloudSyncStatusLine = string.Empty;

    public bool ShowCloudSyncControls =>
        _licenseService.IsCloudSyncImplemented &&
        (_licenseService.CanUseCloudSync || _metadataSyncEnabled);

    public bool CanToggleCloudSync =>
        ShowCloudSyncControls && _licenseService.CanUseCloudSync && !IsBusy;

    public bool ShowSyncPurchaseLaterHint =>
        ShowStorePurchaseControls && !_licenseService.IsCloudSyncImplemented;

    public bool CanBuySync =>
        ShowStorePurchaseControls &&
        _licenseService.IsCloudSyncImplemented &&
        !_licenseService.CanUseCloudSync &&
        !IsBusy;

    public async Task InitializeAsync()
    {
        await _licenseService.RefreshAsync();
        await LoadProductPriceAsync();
        await RefreshCloudSyncFromMetadataAsync();
        RefreshFromService();
    }

    partial void OnCloudSyncEnabledChanged(bool value)
    {
        if (_suppressCloudSyncToggle)
            return;

        CloudSyncToggledCommand.Execute(value);
    }

    [RelayCommand]
    private async Task CloudSyncToggled(bool enable)
    {
        if (!ShowCloudSyncControls)
            return;

        if (IsBusy)
        {
            SetCloudSyncEnabledSilently(_lastKnownCloudSyncEnabled);
            return;
        }

        IsBusy = true;
        try
        {
            if (enable)
                await EnableCloudSyncAsync();
            else
                await DisableCloudSyncAsync();
        }
        catch (Exception ex)
        {
            SetCloudSyncEnabledSilently(_lastKnownCloudSyncEnabled);
            var metadata = await _syncMetadataStore.GetAsync();
            metadata.LastError = ex.Message;
            await _syncMetadataStore.SaveAsync(metadata);
            CloudSyncStatusLine = BuildCloudSyncStatusLine(metadata);
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Sync_Error, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BuyPro()
    {
        if (_licenseService.HasPro)
            return;

        await PurchaseProductAsync(LicenseProductIds.Pro, ResourceKeys.Lic_PurchaseSuccess);
    }

    [RelayCommand]
    private async Task BuySync()
    {
        if (_licenseService.CanUseCloudSync)
            return;

        await PurchaseProductAsync(LicenseProductIds.SyncYearly, ResourceKeys.Lic_SyncPurchaseSuccess);
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
            // Only overwrite cache when StoreKit reports owned products (empty restore keeps cache).
            var owned = await _billingService.GetOwnedProductIdsAsync();
            if (owned.Count > 0)
                await _entitlementStore.ApplyOwnedProductIdsAsync(owned);
            await _licenseService.RefreshAsync();
            await RefreshCloudSyncFromMetadataAsync();
            RefreshFromService();
            await _feedbackService.ShowSnackbarAsync(ok || owned.Count > 0
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
        if (!AreLicenseStubsVisible || _licenseService.Channel != DistributionChannel.Store)
            return;

        await _entitlementStore.SetStubEntitlementsAsync(StubProEnabled, StubSyncEnabled);
        await _licenseService.RefreshAsync();
        await RefreshCloudSyncFromMetadataAsync();
        RefreshFromService();
    }

    [RelayCommand]
    private async Task UseStoreKitInsteadOfStub()
    {
        if (!AreLicenseStubsVisible)
            return;

        await _entitlementStore.ClearStubPreferenceAsync();
        await _licenseService.RefreshAsync();
        await LoadProductPriceAsync();
        await RefreshCloudSyncFromMetadataAsync();
        RefreshFromService();
    }

    private async Task EnableCloudSyncAsync()
    {
        var result = await _enableCloudSyncUseCase.ExecuteAsync();
        if (result.Status == EnableCloudSyncStatus.Enabled)
        {
            await RefreshCloudSyncFromMetadataAsync();
            RefreshFromService();
            await _cloudSyncOrchestrator.SyncNowAsync();
            return;
        }

        SetCloudSyncEnabledSilently(false);
        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Err_Titel),
            _loc.GetString(GetBlockedMessageKey(result.Status)),
            _loc.GetString(ResourceKeys.Btn_OK));
    }

    private async Task DisableCloudSyncAsync()
    {
        var metadata = await _syncMetadataStore.GetAsync();
        metadata.SyncEnabled = false;
        await _syncMetadataStore.SaveAsync(metadata);
        await _cloudSyncOrchestrator.StopAsync();
        await RefreshCloudSyncFromMetadataAsync();
        RefreshFromService();
    }

    private async Task RefreshCloudSyncFromMetadataAsync()
    {
        if (!_licenseService.IsCloudSyncImplemented)
            return;

        var metadata = await _syncMetadataStore.GetAsync();
        _metadataSyncEnabled = metadata.SyncEnabled;
        SetCloudSyncEnabledSilently(metadata.SyncEnabled);
        CloudSyncStatusLine = BuildCloudSyncStatusLine(metadata);
        OnPropertyChanged(nameof(ShowCloudSyncControls));
        OnPropertyChanged(nameof(CanToggleCloudSync));
    }

    private string BuildCloudSyncStatusLine(SyncMetadata metadata)
    {
        if (metadata.SyncEnabled && !_licenseService.CanUseCloudSync)
            return _loc.GetString(ResourceKeys.Sync_PausedRenew);

        if (!string.IsNullOrWhiteSpace(metadata.LastError))
            return _loc.GetString(ResourceKeys.Sync_Error, metadata.LastError);

        if (metadata.LastSyncUtc.HasValue)
        {
            var formatted = metadata.LastSyncUtc.Value.ToLocalTime().ToString("g");
            return _loc.GetString(ResourceKeys.Sync_LastSync, formatted);
        }

        return _loc.GetString(ResourceKeys.Sync_NeverSynced);
    }

    private static string GetBlockedMessageKey(EnableCloudSyncStatus status) => status switch
    {
        EnableCloudSyncStatus.BlockedBothHaveData => ResourceKeys.Sync_BlockedBothHaveData,
        EnableCloudSyncStatus.BlockedNoEntitlement => ResourceKeys.Sync_BlockedNoEntitlement,
        EnableCloudSyncStatus.BlockedUnsupported => ResourceKeys.Sync_BlockedUnsupported,
        EnableCloudSyncStatus.BlockedNoICloud => ResourceKeys.Sync_BlockedNoICloud,
        _ => ResourceKeys.Sync_BlockedUnsupported
    };

    private void SetCloudSyncEnabledSilently(bool value)
    {
        _lastKnownCloudSyncEnabled = value;
        _suppressCloudSyncToggle = true;
        CloudSyncEnabled = value;
        _suppressCloudSyncToggle = false;
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanToggleCloudSync));
        OnPropertyChanged(nameof(CanBuySync));
    }

    private async Task PurchaseProductAsync(string productId, string successKey)
    {
        if (IsBusy || !_billingService.IsAvailable)
            return;

        IsBusy = true;
        try
        {
            var result = await _billingService.PurchaseAsync(productId);
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
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(successKey));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PersistOwnedAndRefreshAsync()
    {
        var owned = await _billingService.GetOwnedProductIdsAsync();
        await _entitlementStore.ApplyOwnedProductIdsAsync(owned);
        await _licenseService.RefreshAsync();
        await RefreshCloudSyncFromMetadataAsync();
        RefreshFromService();
    }

    private async Task LoadProductPriceAsync()
    {
        ProPriceLabel = string.Empty;
        SyncPriceLabel = string.Empty;
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

            var sync = products.FirstOrDefault(p => p.Id == LicenseProductIds.SyncYearly);
            if (sync != null && !string.IsNullOrWhiteSpace(sync.LocalizedPrice))
                SyncPriceLabel = sync.LocalizedPrice;
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
        // Stub toggles: Debug Store builds only (hidden in Release / TestFlight / App Store)
        ShowStoreStubControls = AreLicenseStubsVisible && isStore;

        if (!isStore)
        {
            SyncLabel = _loc.GetString(ResourceKeys.Lic_SyncUnavailableDirect);
            LimitsHint = _loc.GetString(ResourceKeys.Lic_DirectFullLocal);
        }
        else if (!_licenseService.IsCloudSyncImplemented)
        {
            SyncLabel = _licenseService.CanUseCloudSync
                ? _loc.GetString(ResourceKeys.Lic_SyncEntitledComingSoon)
                : _loc.GetString(ResourceKeys.Lic_SyncComingSoon);
            LimitsHint = _licenseService.HasPro
                ? string.Empty
                : _loc.GetString(ResourceKeys.Lic_FreeLimitsHint);
        }
        else if (_licenseService.CanUseCloudSync)
        {
            SyncLabel = CloudSyncEnabled
                ? _loc.GetString(ResourceKeys.Lic_SyncActive)
                : _loc.GetString(ResourceKeys.Lic_SyncInactive);
            LimitsHint = _licenseService.HasPro
                ? string.Empty
                : _loc.GetString(ResourceKeys.Lic_FreeLimitsHint);
        }
        else
        {
            SyncLabel = _loc.GetString(ResourceKeys.Lic_SyncAvailable);
            LimitsHint = _licenseService.HasPro
                ? string.Empty
                : _loc.GetString(ResourceKeys.Lic_FreeLimitsHint);
        }

        StubProEnabled = _licenseService.HasPro && isStore;
        StubSyncEnabled = _licenseService.HasSyncSubscription;

        OnPropertyChanged(nameof(ShowCloudSyncControls));
        OnPropertyChanged(nameof(CanToggleCloudSync));
        OnPropertyChanged(nameof(ShowSyncPurchaseLaterHint));
        OnPropertyChanged(nameof(CanBuySync));
    }

    /// <summary>Dev-only entitlement stubs must never appear in Release Store builds.</summary>
    private static bool AreLicenseStubsVisible =>
#if DEBUG
        true;
#else
        false;
#endif
}
