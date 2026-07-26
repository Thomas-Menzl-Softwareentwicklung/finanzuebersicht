namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// Store entitlement source (StoreKit later). Direct builds do not use this.
/// Stub may persist debug flags in settings for Store-test builds.
/// </summary>
public interface ILicenseEntitlementStore
{
    Task<(bool HasPro, bool HasSyncSubscription)> GetEntitlementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Debug/stub only until StoreKit is wired.</summary>
    Task SetStubEntitlementsAsync(bool hasPro, bool hasSyncSubscription, CancellationToken cancellationToken = default);
}
