namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// Entitlement source for Store builds. Backed by StoreKit when available, with settings cache/stub fallback.
/// </summary>
public interface ILicenseEntitlementStore
{
    Task<(bool HasPro, bool HasSyncSubscription)> GetEntitlementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist ownership resolved from StoreKit (after purchase/restore).
    /// Empty lists are ignored so a failed/empty restore cannot wipe the offline cache.
    /// </summary>
    Task ApplyOwnedProductIdsAsync(IEnumerable<string> ownedProductIds, CancellationToken cancellationToken = default);

    /// <summary>Debug/stub override for local Store builds and unit tests. No-op when stubs are disabled (Release).</summary>
    Task SetStubEntitlementsAsync(bool hasPro, bool hasSyncSubscription, CancellationToken cancellationToken = default);

    /// <summary>Stop preferring stub flags; next refresh uses StoreKit/cache.</summary>
    Task ClearStubPreferenceAsync(CancellationToken cancellationToken = default);
}
