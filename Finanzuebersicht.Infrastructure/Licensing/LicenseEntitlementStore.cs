using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Infrastructure.Licensing;

/// <summary>
/// Resolves Pro/Sync from StoreKit when available; caches in settings for offline.
/// Stub setters remain for Store sandbox debugging when products are not yet configured.
/// </summary>
public sealed class LicenseEntitlementStore(
    ISettingsService settings,
    IStoreBillingService billingService) : ILicenseEntitlementStore
{
    public const string ProKey = SettingsKeys.LicenseStubHasPro;
    public const string SyncKey = SettingsKeys.LicenseStubHasSync;
    public const string CacheProKey = "License.Cache.HasPro";
    public const string CacheSyncKey = "License.Cache.HasSync";
    public const string PreferStubKey = "License.PreferStub";

    public async Task<(bool HasPro, bool HasSyncSubscription)> GetEntitlementsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsTrue(settings.Get(PreferStubKey, "false")) || !billingService.IsAvailable)
        {
            return (
                IsTrue(settings.Get(ProKey, settings.Get(CacheProKey, "false"))),
                IsTrue(settings.Get(SyncKey, settings.Get(CacheSyncKey, "false"))));
        }

        try
        {
            if (!await billingService.InitializeAsync(cancellationToken).ConfigureAwait(false))
                return ReadCache();

            // Do not call Restore here (would prompt). Use in-memory/pending transactions + cache.
            var owned = await billingService.GetOwnedProductIdsAsync(cancellationToken).ConfigureAwait(false);
            if (owned.Count > 0)
            {
                var hasPro = Contains(owned, LicenseProductIds.Pro);
                var hasSync = Contains(owned, LicenseProductIds.SyncYearly);
                WriteCache(hasPro, hasSync);
                return (hasPro, hasSync);
            }

            return ReadCache();
        }
        catch
        {
            return ReadCache();
        }
    }

    public Task ApplyOwnedProductIdsAsync(IEnumerable<string> ownedProductIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owned = ownedProductIds as ICollection<string> ?? ownedProductIds.ToList();
        var hasPro = Contains(owned, LicenseProductIds.Pro);
        var hasSync = Contains(owned, LicenseProductIds.SyncYearly);
        settings.Set(PreferStubKey, "false");
        WriteCache(hasPro, hasSync);
        return Task.CompletedTask;
    }

    public Task SetStubEntitlementsAsync(bool hasPro, bool hasSyncSubscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        settings.Set(PreferStubKey, "true");
        settings.Set(ProKey, hasPro ? "true" : "false");
        settings.Set(SyncKey, hasSyncSubscription ? "true" : "false");
        WriteCache(hasPro, hasSyncSubscription);
        return Task.CompletedTask;
    }

    public Task ClearStubPreferenceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        settings.Set(PreferStubKey, "false");
        return Task.CompletedTask;
    }

    private (bool HasPro, bool HasSyncSubscription) ReadCache() => (
        IsTrue(settings.Get(CacheProKey, "false")),
        IsTrue(settings.Get(CacheSyncKey, "false")));

    private void WriteCache(bool hasPro, bool hasSync)
    {
        settings.Set(CacheProKey, hasPro ? "true" : "false");
        settings.Set(CacheSyncKey, hasSync ? "true" : "false");
    }

    private static bool Contains(IEnumerable<string> owned, string productId)
        => owned.Contains(productId, StringComparer.Ordinal);

    private static bool IsTrue(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
