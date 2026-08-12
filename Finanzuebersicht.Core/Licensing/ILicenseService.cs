namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// Resolves Free / Pro / Sync entitlements and soft limits.
/// Direct builds are always Pro locally and never expose Cloud Sync.
/// </summary>
public interface ILicenseService
{
    DistributionChannel Channel { get; }

    /// <summary>Pro unlock (or Direct build). Removes soft limits and unlocks Pro features.</summary>
    bool HasPro { get; }

    /// <summary>Active Sync subscription entitlement (Store only; ignored on Direct).</summary>
    bool HasSyncSubscription { get; }

    /// <summary>
    /// Whether this build may offer Cloud Sync at all.
    /// Always false for Direct (Windows / sideload Mac). True for Store when subscribed.
    /// Does not imply CloudKit is implemented yet.
    /// </summary>
    bool CanUseCloudSync { get; }

    /// <summary>CloudKit engine ready for use (false until #243).</summary>
    bool IsCloudSyncImplemented { get; }

    bool HasFeature(AppFeature feature);

    LimitCheckResult CheckCreateLimit(LimitedResource resource, int currentCount);

    void EnsureCanCreate(LimitedResource resource, int currentCount);

    void EnsureFeature(AppFeature feature);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
