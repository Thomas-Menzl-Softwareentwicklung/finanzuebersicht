using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Infrastructure.Licensing;

/// <summary>Persists stub Store entitlements until StoreKit is implemented.</summary>
public sealed class SettingsLicenseEntitlementStore(ISettingsService settings) : ILicenseEntitlementStore
{
    public const string ProKey = "License.Stub.HasPro";
    public const string SyncKey = "License.Stub.HasSync";

    public Task<(bool HasPro, bool HasSyncSubscription)> GetEntitlementsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hasPro = string.Equals(settings.Get(ProKey, "false"), "true", StringComparison.OrdinalIgnoreCase);
        var hasSync = string.Equals(settings.Get(SyncKey, "false"), "true", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult((hasPro, hasSync));
    }

    public Task SetStubEntitlementsAsync(bool hasPro, bool hasSyncSubscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        settings.Set(ProKey, hasPro ? "true" : "false");
        settings.Set(SyncKey, hasSyncSubscription ? "true" : "false");
        return Task.CompletedTask;
    }
}
