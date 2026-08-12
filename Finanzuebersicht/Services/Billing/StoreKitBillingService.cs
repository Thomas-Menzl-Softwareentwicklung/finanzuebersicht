#if IOS || MACCATALYST
using Foundation;
using Microsoft.Extensions.Logging;
using StoreKit;
using Finanzuebersicht.Core.Licensing;

namespace Finanzuebersicht.Services.Billing;

/// <summary>
/// StoreKit 1 billing for iOS / Mac Catalyst Store builds.
/// StoreKit 2 awaits fuller .NET Swift interop; StoreKit 1 remains supported (Microsoft MAUI sample pattern).
/// </summary>
#pragma warning disable CA1422
public sealed class StoreKitBillingService : IStoreBillingService
{
    private readonly ILogger<StoreKitBillingService> _logger;
    private readonly HashSet<string> _ownedProducts = new(StringComparer.Ordinal);
    private PaymentTransactionObserver? _paymentObserver;
    private TaskCompletionSource<StorePurchaseResult>? _purchaseTcs;
    private bool _initialized;

    public StoreKitBillingService(ILogger<StoreKitBillingService> logger)
    {
        _logger = logger;
    }

    public bool IsAvailable => true;

    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_initialized)
            return Task.FromResult(true);

        try
        {
            if (!SKPaymentQueue.CanMakePayments)
            {
                _logger.LogWarning("SKPaymentQueue.CanMakePayments is false");
                return Task.FromResult(false);
            }

            _paymentObserver ??= new PaymentTransactionObserver(this);
            SKPaymentQueue.DefaultQueue.AddTransactionObserver(_paymentObserver);
            _initialized = true;
            _logger.LogInformation("StoreKit billing initialized");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StoreKit initialization failed");
            return Task.FromResult(false);
        }
    }

    public async Task<IReadOnlyList<StoreProductInfo>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        var products = await QueryStoreProductsAsync(LicenseProductIds.All, cancellationToken).ConfigureAwait(false);
        return products.Select(p => new StoreProductInfo
        {
            Id = p.ProductIdentifier,
            Title = p.LocalizedTitle ?? p.ProductIdentifier,
            Description = p.LocalizedDescription ?? string.Empty,
            LocalizedPrice = p.PriceLocale != null
                ? $"{p.Price} {p.PriceLocale.CurrencyCode}"
                : p.Price.ToString(),
            IsOwned = _ownedProducts.Contains(p.ProductIdentifier)
        }).ToList();
    }

    public async Task<IReadOnlyList<string>> GetOwnedProductIdsAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        // Ownership is tracked from transactions + restore. Also finish any pending queue items.
        foreach (var transaction in SKPaymentQueue.DefaultQueue.Transactions ?? [])
        {
            HandleTransaction(transaction, completePurchaseTcs: false);
        }

        return _ownedProducts.ToList();
    }

    public async Task<StorePurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (!_initialized || !SKPaymentQueue.CanMakePayments)
            return StorePurchaseResult.Failed(productId, "In-app purchases are disabled on this device.");

        try
        {
            var products = await QueryStoreProductsAsync([productId], cancellationToken).ConfigureAwait(false);
            var product = products.FirstOrDefault();
            if (product == null)
                return StorePurchaseResult.Failed(productId, "Product not found in App Store. Create it in App Store Connect and use a Sandbox Apple ID.");

            _purchaseTcs = new TaskCompletionSource<StorePurchaseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = cancellationToken.Register(() =>
                _purchaseTcs.TrySetResult(StorePurchaseResult.Cancelled(productId)));

            var payment = SKPayment.CreateFrom(product);
            SKPaymentQueue.DefaultQueue.AddPayment(payment);
            return await _purchaseTcs.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Purchase failed for {ProductId}", productId);
            return StorePurchaseResult.Failed(productId, ex.Message);
        }
    }

    public async Task<bool> RestorePurchasesAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (_paymentObserver == null)
            return false;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnFinished(object? sender, EventArgs e) => tcs.TrySetResult(true);
        void OnFailed(object? sender, NSError error)
        {
            if (error.Code == 2)
                _logger.LogInformation("Restore cancelled by user");
            else
                _logger.LogWarning("Restore failed: {Code} {Message}", error.Code, error.LocalizedDescription);
            tcs.TrySetResult(false);
        }

        _paymentObserver.RestoreCompletedTransactionsFinishedEvent += OnFinished;
        _paymentObserver.RestoreCompletedTransactionsFailedEvent += OnFailed;
        try
        {
            using var reg = cancellationToken.Register(() => tcs.TrySetResult(false));
            SKPaymentQueue.DefaultQueue.RestoreCompletedTransactions();
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _paymentObserver.RestoreCompletedTransactionsFinishedEvent -= OnFinished;
            _paymentObserver.RestoreCompletedTransactionsFailedEvent -= OnFailed;
        }
    }

    internal void OnTransactionUpdated(SKPaymentTransaction transaction)
        => HandleTransaction(transaction, completePurchaseTcs: true);

    private void HandleTransaction(SKPaymentTransaction transaction, bool completePurchaseTcs)
    {
        var productId = transaction.Payment?.ProductIdentifier ?? string.Empty;

        switch (transaction.TransactionState)
        {
            case SKPaymentTransactionState.Purchased:
                if (!string.IsNullOrEmpty(productId))
                    _ownedProducts.Add(productId);
                SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);
                if (completePurchaseTcs)
                    _purchaseTcs?.TrySetResult(StorePurchaseResult.Success(productId));
                break;

            case SKPaymentTransactionState.Restored:
                if (!string.IsNullOrEmpty(productId))
                    _ownedProducts.Add(productId);
                SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);
                break;

            case SKPaymentTransactionState.Failed:
                SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);
                if (completePurchaseTcs)
                {
                    // SKError.PaymentCancelled == 2
                    var cancelled = transaction.Error?.Code == 2;
                    _purchaseTcs?.TrySetResult(cancelled
                        ? StorePurchaseResult.Cancelled(productId)
                        : StorePurchaseResult.Failed(productId, transaction.Error?.LocalizedDescription ?? "Purchase failed"));
                }
                break;
        }
    }

    private static async Task<List<SKProduct>> QueryStoreProductsAsync(
        IReadOnlyList<string> productIds,
        CancellationToken cancellationToken)
    {
        var identifiers = NSSet.MakeNSObjectSet(productIds.Select(id => new NSString(id)).ToArray());
        var requestDelegate = new ProductsRequestDelegate();
        var request = new SKProductsRequest(identifiers) { Delegate = requestDelegate };
        request.Start();
        using var reg = cancellationToken.Register(() =>
            requestDelegate.TryCancel(new OperationCanceledException(cancellationToken)));
        return await requestDelegate.GetProductsAsync().ConfigureAwait(false);
    }
}

internal sealed class ProductsRequestDelegate : NSObject, ISKProductsRequestDelegate, ISKRequestDelegate
{
    private readonly TaskCompletionSource<List<SKProduct>> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<List<SKProduct>> GetProductsAsync() => _tcs.Task;

    public void TryCancel(Exception ex) => _tcs.TrySetException(ex);

    [Export("productsRequest:didReceiveResponse:")]
    public void ReceivedResponse(SKProductsRequest request, SKProductsResponse response)
        => _tcs.TrySetResult(response.Products?.ToList() ?? []);

    [Export("request:didFailWithError:")]
    public void RequestFailed(SKRequest request, NSError error)
        => _tcs.TrySetException(new InvalidOperationException(error.LocalizedDescription));
}

internal sealed class PaymentTransactionObserver : SKPaymentTransactionObserver
{
    private readonly StoreKitBillingService _billing;

    public event EventHandler? RestoreCompletedTransactionsFinishedEvent;
    public event EventHandler<NSError>? RestoreCompletedTransactionsFailedEvent;

    public PaymentTransactionObserver(StoreKitBillingService billing) => _billing = billing;

    public override void UpdatedTransactions(SKPaymentQueue queue, SKPaymentTransaction[] transactions)
    {
        foreach (var transaction in transactions)
            _billing.OnTransactionUpdated(transaction);
    }

    public override void RestoreCompletedTransactionsFinished(SKPaymentQueue queue)
        => RestoreCompletedTransactionsFinishedEvent?.Invoke(this, EventArgs.Empty);

    public override void RestoreCompletedTransactionsFailedWithError(SKPaymentQueue queue, NSError error)
        => RestoreCompletedTransactionsFailedEvent?.Invoke(this, error);
}
#pragma warning restore CA1422
#endif
