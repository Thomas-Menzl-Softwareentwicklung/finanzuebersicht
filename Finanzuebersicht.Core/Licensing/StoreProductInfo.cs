namespace Finanzuebersicht.Core.Licensing;

public sealed class StoreProductInfo
{
    public required string Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string LocalizedPrice { get; init; } = string.Empty;
    public bool IsOwned { get; init; }
}
