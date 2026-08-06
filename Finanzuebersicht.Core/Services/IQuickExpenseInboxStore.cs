namespace Finanzuebersicht.Core.Services;

public sealed record QuickExpenseInboxItem(
    string Id,
    string AmountText,
    string Title,
    DateTimeOffset CreatedAt);

/// <summary>
/// Pending quick expenses written by the iOS widget (App Group) or tests.
/// </summary>
public interface IQuickExpenseInboxStore
{
    Task<IReadOnlyList<QuickExpenseInboxItem>> DrainPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the pending file (used to put back items that could not be processed).</summary>
    Task WritePendingAsync(IReadOnlyList<QuickExpenseInboxItem> items, CancellationToken cancellationToken = default);
}
