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
    private readonly ILocalizationService _loc;

    public LicenseViewModel(
        ILicenseService licenseService,
        ILicenseEntitlementStore entitlementStore,
        ILocalizationService localizationService)
    {
        _licenseService = licenseService;
        _entitlementStore = entitlementStore;
        _loc = localizationService;
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
    private bool showStoreStubControls;

    [ObservableProperty]
    private bool stubProEnabled;

    [ObservableProperty]
    private bool stubSyncEnabled;

    public async Task InitializeAsync()
    {
        await _licenseService.RefreshAsync();
        RefreshFromService();
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

    private void RefreshFromService()
    {
        ChannelLabel = _licenseService.Channel == DistributionChannel.Store
            ? _loc.GetString(ResourceKeys.Lic_ChannelStore)
            : _loc.GetString(ResourceKeys.Lic_ChannelDirect);

        TierLabel = _licenseService.HasPro
            ? _loc.GetString(ResourceKeys.Lic_TierPro)
            : _loc.GetString(ResourceKeys.Lic_TierFree);

        if (_licenseService.Channel == DistributionChannel.Direct)
        {
            SyncLabel = _loc.GetString(ResourceKeys.Lic_SyncUnavailableDirect);
            LimitsHint = _loc.GetString(ResourceKeys.Lic_DirectFullLocal);
            ShowStoreStubControls = false;
        }
        else if (_licenseService.CanUseCloudSync)
        {
            SyncLabel = _licenseService.IsCloudSyncImplemented
                ? _loc.GetString(ResourceKeys.Lic_SyncActive)
                : _loc.GetString(ResourceKeys.Lic_SyncEntitledComingSoon);
            LimitsHint = _licenseService.HasPro
                ? string.Empty
                : _loc.GetString(ResourceKeys.Lic_FreeLimitsHint);
            ShowStoreStubControls = true;
        }
        else
        {
            SyncLabel = _loc.GetString(ResourceKeys.Lic_SyncInactive);
            LimitsHint = _licenseService.HasPro
                ? string.Empty
                : _loc.GetString(ResourceKeys.Lic_FreeLimitsHint);
            ShowStoreStubControls = true;
        }

        StubProEnabled = _licenseService.HasPro && _licenseService.Channel == DistributionChannel.Store;
        StubSyncEnabled = _licenseService.HasSyncSubscription;
    }
}
