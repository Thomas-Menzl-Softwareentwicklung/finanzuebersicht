using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Backup;
using Finanzuebersicht.Application.UseCases.Sync;
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
    private readonly IExternalBrowser _externalBrowser;
    private readonly LicenseBillingCoordinator _billingCoordinator;
    private readonly LicenseCloudSyncCoordinator _cloudSync;
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
        ClearLocalSyncedDataUseCase clearLocalSyncedDataUseCase,
        DisableCloudSyncUseCase disableCloudSyncUseCase,
        CreateBackupUseCase createBackupUseCase,
        GetCloudSyncStatusUseCase getCloudSyncStatusUseCase,
        RecordCloudSyncErrorUseCase recordCloudSyncErrorUseCase,
        StartCloudSyncUseCase startCloudSyncUseCase,
        IAppEvents appEvents,
        IExternalBrowser externalBrowser)
    {
        _licenseService = licenseService;
        _entitlementStore = entitlementStore;
        _billingService = billingService;
        _loc = localizationService;
        _dialogService = dialogService;
        _externalBrowser = externalBrowser;
        _billingCoordinator = new LicenseBillingCoordinator(
            billingService,
            entitlementStore,
            licenseService,
            dialogService,
            feedbackService,
            localizationService);
        _cloudSync = new LicenseCloudSyncCoordinator(
            licenseService,
            localizationService,
            dialogService,
            enableCloudSyncUseCase,
            clearLocalSyncedDataUseCase,
            disableCloudSyncUseCase,
            createBackupUseCase,
            getCloudSyncStatusUseCase,
            recordCloudSyncErrorUseCase,
            startCloudSyncUseCase,
            appEvents);
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

    public bool ShowLegalLinks => _licenseService.Channel == DistributionChannel.Store;

    public async Task InitializeAsync()
    {
        await _licenseService.RefreshAsync();
        await ApplyProductPricesAsync();
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
            var outcome = await _cloudSync.ToggleAsync(enable, _lastKnownCloudSyncEnabled);
            ApplyCloudSyncToggle(outcome);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenPrivacyPolicy() =>
        await OpenLegalUrlAsync(ResolvePrivacyPolicyUrl());

    [RelayCommand]
    private async Task OpenTermsOfUse() =>
        await OpenLegalUrlAsync(StoreLegalUrls.AppleStandardEula);

    [RelayCommand]
    private async Task BuyPro()
    {
        if (_licenseService.HasPro)
            return;

        await _billingCoordinator.PurchaseProductAsync(
            LicenseProductIds.Pro,
            ResourceKeys.Lic_PurchaseSuccess,
            () => IsBusy,
            v => IsBusy = v,
            OnBillingEntitlementsRefreshedAsync);
    }

    [RelayCommand]
    private async Task BuySync()
    {
        if (_licenseService.CanUseCloudSync)
            return;

        await _billingCoordinator.PurchaseProductAsync(
            LicenseProductIds.SyncYearly,
            ResourceKeys.Lic_SyncPurchaseSuccess,
            () => IsBusy,
            v => IsBusy = v,
            OnBillingEntitlementsRefreshedAsync);
    }

    [RelayCommand]
    private Task RestorePurchases() =>
        _billingCoordinator.RestorePurchasesAsync(
            () => IsBusy,
            v => IsBusy = v,
            OnBillingEntitlementsRefreshedAsync);

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
        await ApplyProductPricesAsync();
        await RefreshCloudSyncFromMetadataAsync();
        RefreshFromService();
    }

    private async Task RefreshCloudSyncFromMetadataAsync()
    {
        var state = await _cloudSync.TryReadStateAsync();
        if (state is null)
            return;

        _metadataSyncEnabled = state.SyncEnabled;
        SetCloudSyncEnabledSilently(state.SyncEnabled);
        CloudSyncStatusLine = state.StatusLine;
        OnPropertyChanged(nameof(ShowCloudSyncControls));
        OnPropertyChanged(nameof(CanToggleCloudSync));
    }

    private void ApplyCloudSyncToggle(LicenseCloudSyncToggleOutcome outcome)
    {
        SetCloudSyncEnabledSilently(outcome.SwitchEnabled);
        if (outcome.StatusLine is not null)
            CloudSyncStatusLine = outcome.StatusLine;
        if (outcome.MetadataSyncEnabled is bool enabled)
        {
            _metadataSyncEnabled = enabled;
            OnPropertyChanged(nameof(ShowCloudSyncControls));
            OnPropertyChanged(nameof(CanToggleCloudSync));
        }

        if (outcome.RefreshLicenseLabels)
            RefreshFromService();
    }

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

    private async Task OnBillingEntitlementsRefreshedAsync()
    {
        await RefreshCloudSyncFromMetadataAsync();
        RefreshFromService();
    }

    private async Task ApplyProductPricesAsync()
    {
        var prices = await _billingCoordinator.LoadProductPricesAsync();
        ProPriceLabel = prices.ProPriceLabel;
        SyncPriceLabel = prices.SyncPriceLabel;
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
        OnPropertyChanged(nameof(ShowLegalLinks));
    }

    private string ResolvePrivacyPolicyUrl()
    {
        var code = _loc.CurrentLanguageCode ?? string.Empty;
        if (code.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return StoreLegalUrls.PrivacyPolicyEn;

        if (string.IsNullOrEmpty(code) &&
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
            return StoreLegalUrls.PrivacyPolicyEn;

        return StoreLegalUrls.PrivacyPolicyDe;
    }

    private async Task OpenLegalUrlAsync(string url)
    {
        try
        {
            await _externalBrowser.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                ex.Message,
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    /// <summary>Dev-only entitlement stubs must never appear in Release Store builds.</summary>
    private static bool AreLicenseStubsVisible =>
#if DEBUG
        true;
#else
        false;
#endif
}
