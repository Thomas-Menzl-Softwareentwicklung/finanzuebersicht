namespace Finanzuebersicht.Core.Licensing;

public sealed class StorePurchaseResult
{
    public bool IsSuccess { get; init; }
    public bool WasCancelled { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static StorePurchaseResult Success(string productId) => new()
    {
        IsSuccess = true,
        ProductId = productId
    };

    public static StorePurchaseResult Cancelled(string productId) => new()
    {
        IsSuccess = false,
        WasCancelled = true,
        ProductId = productId
    };

    public static StorePurchaseResult Failed(string productId, string message) => new()
    {
        IsSuccess = false,
        ProductId = productId,
        ErrorMessage = message
    };
}
