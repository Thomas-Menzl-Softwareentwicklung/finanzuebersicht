namespace Finanzuebersicht.Core.Licensing;

/// <summary>No-op license used when no gate is registered (tests / tooling).</summary>
public sealed class UnrestrictedLicenseService : ILicenseService
{
    public static UnrestrictedLicenseService Instance { get; } = new();

    private UnrestrictedLicenseService() { }

    public DistributionChannel Channel => DistributionChannel.Direct;
    public bool HasPro => true;
    public bool HasSyncSubscription => false;
    public bool CanUseCloudSync => false;
    public bool IsCloudSyncImplemented => false;

    public bool HasFeature(AppFeature feature) => feature != AppFeature.CloudSync;

    public LimitCheckResult CheckCreateLimit(LimitedResource resource, int currentCount)
        => LimitCheckResult.Unlimited(currentCount);

    public void EnsureCanCreate(LimitedResource resource, int currentCount) { }

    public void EnsureFeature(AppFeature feature)
    {
        if (feature == AppFeature.CloudSync)
            throw new FeatureGateException(feature, "Cloud Sync is not available for Direct builds.");
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
