using Finanzuebersicht.Core.Licensing;

namespace Finanzuebersicht.Infrastructure.Licensing;

public sealed class LicenseService : ILicenseService
{
    private readonly IDistributionChannelProvider _channelProvider;
    private readonly ILicenseEntitlementStore _entitlementStore;
    private bool _hasPro;
    private bool _hasSyncSubscription;
    private bool _loaded;

    public LicenseService(
        IDistributionChannelProvider channelProvider,
        ILicenseEntitlementStore entitlementStore)
    {
        _channelProvider = channelProvider;
        _entitlementStore = entitlementStore;
        ApplyChannelDefaults();
    }

    public DistributionChannel Channel => _channelProvider.Channel;

    public bool HasPro
    {
        get
        {
            EnsureLoaded();
            return Channel == DistributionChannel.Direct || _hasPro;
        }
    }

    public bool HasSyncSubscription
    {
        get
        {
            EnsureLoaded();
            return Channel == DistributionChannel.Store && _hasSyncSubscription;
        }
    }

    public bool CanUseCloudSync => Channel == DistributionChannel.Store && HasSyncSubscription;

    /// <inheritdoc />
    public bool IsCloudSyncImplemented => false;

    public bool HasFeature(AppFeature feature) => feature switch
    {
        AppFeature.CsvImport => HasPro,
        AppFeature.Cashflow => HasPro,
        AppFeature.CloudSync => CanUseCloudSync,
        AppFeature.QuickExpenseCapture => HasPro, // iOS widget only; in-app Schnell is free
        _ => false
    };

    public LimitCheckResult CheckCreateLimit(LimitedResource resource, int currentCount)
    {
        if (HasPro)
            return LimitCheckResult.Unlimited(currentCount);

        var limit = FreeTierLimits.GetMax(resource);
        return new LimitCheckResult(currentCount < limit, currentCount, limit);
    }

    public void EnsureCanCreate(LimitedResource resource, int currentCount)
    {
        var check = CheckCreateLimit(resource, currentCount);
        if (check.Allowed)
            return;

        throw new FeatureGateException(
            resource,
            currentCount,
            check.Limit!.Value,
            $"Free limit reached for {resource}: {currentCount}/{check.Limit}.");
    }

    public void EnsureFeature(AppFeature feature)
    {
        if (HasFeature(feature))
            return;

        throw new FeatureGateException(feature, $"Feature {feature} requires Pro or Sync entitlement.");
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (Channel == DistributionChannel.Direct)
        {
            ApplyChannelDefaults();
            _loaded = true;
            return;
        }

        var (hasPro, hasSync) = await _entitlementStore.GetEntitlementsAsync(cancellationToken).ConfigureAwait(false);
        _hasPro = hasPro;
        _hasSyncSubscription = hasSync;
        _loaded = true;
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        // Sync path for first access before RefreshAsync (startup may call Refresh).
        if (Channel == DistributionChannel.Direct)
        {
            ApplyChannelDefaults();
            _loaded = true;
            return;
        }

        // Store: never block the UI thread on StoreKit (sync-over-async deadlocks → watchdog kill).
        // Constructor defaults stay in effect until RefreshAsync completes.
    }

    private void ApplyChannelDefaults()
    {
        if (Channel == DistributionChannel.Direct)
        {
            _hasPro = true;
            _hasSyncSubscription = false;
        }
        else
        {
            _hasPro = false;
            _hasSyncSubscription = false;
        }
    }
}
