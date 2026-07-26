namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// Platform billing (StoreKit on Apple Store builds). Direct/Windows use an unavailable implementation.
/// </summary>
public interface IStoreBillingService
{
    /// <summary>False when billing is not supported on this build/platform.</summary>
    bool IsAvailable { get; }

    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoreProductInfo>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetOwnedProductIdsAsync(CancellationToken cancellationToken = default);

    Task<StorePurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default);

    Task<bool> RestorePurchasesAsync(CancellationToken cancellationToken = default);
}
