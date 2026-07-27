namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// Entitlement source for Store builds. Backed by StoreKit when available, with settings cache/stub fallback.
/// </summary>
public interface ILicenseEntitlementStore
{
    Task<(bool HasPro, bool HasSyncSubscription)> GetEntitlementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persist ownership resolved from StoreKit (after purchase/restore).</summary>
    Task ApplyOwnedProductIdsAsync(IEnumerable<string> ownedProductIds, CancellationToken cancellationToken = default);

    /// <summary>Debug/stub override until App Store products exist.</summary>
    Task SetStubEntitlementsAsync(bool hasPro, bool hasSyncSubscription, CancellationToken cancellationToken = default);

    /// <summary>Stop preferring stub flags; next refresh uses StoreKit/cache.</summary>
    Task ClearStubPreferenceAsync(CancellationToken cancellationToken = default);
}
