namespace Finanzuebersicht.Core.Licensing;

/// <summary>No-op billing for Direct builds, Windows, and simulators without StoreKit.</summary>
public sealed class UnavailableStoreBillingService : IStoreBillingService
{
    public bool IsAvailable => false;

    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<StoreProductInfo>> GetProductsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<StoreProductInfo>>([]);

    public Task<IReadOnlyList<string>> GetOwnedProductIdsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<StorePurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        => Task.FromResult(StorePurchaseResult.Failed(productId, "In-app purchases are not available in this build."));

    public Task<bool> RestorePurchasesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
