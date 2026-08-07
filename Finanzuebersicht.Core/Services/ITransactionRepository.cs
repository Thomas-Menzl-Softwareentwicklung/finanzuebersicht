using Finanzuebersicht.Models;

namespace Finanzuebersicht.Core.Services;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetTransactionsAsync(DateTime vonDatum, DateTime bisDatum);

    /// <summary>
    /// Loads all transactions without a date-range filter. Prefer targeted queries
    /// (<see cref="GetEarliestTransactionYearAsync"/>, <see cref="HasTransactionsForCategoryAsync"/>, …)
    /// when a full scan is not required. Intended for backup/export paths.
    /// </summary>
    Task<List<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the earliest transaction year, or null when there are no transactions.</summary>
    Task<int?> GetEarliestTransactionYearAsync(CancellationToken cancellationToken = default);

    Task<bool> HasTransactionsForCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<bool> HasTransactionsForAccountAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remaps <paramref name="fromCategoryId"/> → <paramref name="toCategoryId"/> in one load/save pass.
    /// Returns the number of updated rows.
    /// </summary>
    Task<int> RemapCategoryIdAsync(string fromCategoryId, string toCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remaps <paramref name="fromAccountId"/> → <paramref name="toAccountId"/> in one load/save pass.
    /// Returns the number of updated rows.
    /// </summary>
    Task<int> RemapAccountIdAsync(string fromAccountId, string toAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns <paramref name="defaultAccountId"/> to transactions with a missing AccountId.
    /// Returns the number of updated rows.
    /// </summary>
    Task<int> AssignMissingAccountIdsAsync(string defaultAccountId, CancellationToken cancellationToken = default);

    Task SaveTransactionAsync(Transaction transaction);
    Task SaveTransactionsAsync(IEnumerable<Transaction> transactions);
    Task DeleteTransactionAsync(string id);
    Task DeleteTransferGroupAsync(string transferGroupId);
    Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> transactions);

    /// <summary>
    /// Finds the most common (non-Unkategorisiert) category for a given payee name.
    /// Uses case-insensitive matching and returns the category with highest frequency
    /// if it exceeds the confidence threshold (default 50%).
    /// </summary>
    /// <param name="payee">Payee name to search for</param>
    /// <param name="confidenceThreshold">Minimum ratio (0.0-1.0) of matching transactions that must share the same category. Default: 0.5 (50%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The most common category for this payee, or null if no match or threshold not met</returns>
    Task<Category?> GetMostCommonCategoryForPayeeAsync(
        string payee,
        double confidenceThreshold = 0.5,
        CancellationToken cancellationToken = default);
}
