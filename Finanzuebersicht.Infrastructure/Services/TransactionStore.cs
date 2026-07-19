using System.Linq;
using Finanzuebersicht.Models;
using Finanzuebersicht.Constants;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// JSON-based adapter for Transaction repository operations.
/// Handles persistence of transactions with thread-safe CRUD operations
/// and smart category inference for payees.
/// </summary>
public class TransactionStore : JsonDataStoreBase, ITransactionRepository
{
    private string TransactionsFile => Path.Combine(DataDir, DataFileNames.Transactions);
    private readonly CategoryStore? _categoryStore;

    public TransactionStore(string dataDir, ILogger<TransactionStore>? logger = null, CategoryStore? categoryStore = null)
        : base(dataDir, logger)
    {
        _categoryStore = categoryStore;
    }

    public async Task<List<Transaction>> GetTransactionsAsync(DateTime vonDatum, DateTime bisDatum)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Transaction>(TransactionsFile);
            return [..items
                .Where(t => t.Datum >= vonDatum && t.Datum <= bisDatum)
                .OrderByDescending(t => t.Datum)];
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task SaveTransactionAsync(Transaction transaction)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Transaction>(TransactionsFile);
            var idx = items.FindIndex(t => t.Id == transaction.Id);
            if (idx >= 0)
                items[idx] = transaction;
            else
                items.Add(transaction);
            await SaveAsync(TransactionsFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task SaveTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Transaction>(TransactionsFile);
            foreach (var transaction in transactions)
            {
                var idx = items.FindIndex(t => t.Id == transaction.Id);
                if (idx >= 0)
                    items[idx] = transaction;
                else
                    items.Add(transaction);
            }
            await SaveAsync(TransactionsFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task DeleteTransactionAsync(string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Transaction>(TransactionsFile);
            items.RemoveAll(t => t.Id == id);
            await SaveAsync(TransactionsFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task DeleteTransferGroupAsync(string transferGroupId)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Transaction>(TransactionsFile);
            items.RemoveAll(t => t.TransferGroupId == transferGroupId);
            await SaveAsync(TransactionsFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> transactions)
        => ReplaceAllAsync(TransactionsFile, transactions);

    /// <summary>
    /// Finds the most common category for a given payee name (case-insensitive).
    /// Only considers non-Unkategorisiert categories with a confidence threshold.
    /// Uses CategoryStore when available to avoid race conditions on shared files.
    /// Respects the provided CancellationToken for async operations.
    /// </summary>
    public async Task<Category?> GetMostCommonCategoryForPayeeAsync(
        string payee,
        double confidenceThreshold = 0.5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payee))
            return null;

        await StoreLock.WaitAsync(cancellationToken);
        try
        {
            // Honor cancellation before loading transactions
            cancellationToken.ThrowIfCancellationRequested();

            var transactions = await LoadAsync<Transaction>(TransactionsFile);

            // Normalize payee for case-insensitive matching
            var normalizedPayee = payee.Trim();

            // Find transactions with matching payee (case-insensitive)
            var matchingTransactions = transactions
                .Where(t => !string.IsNullOrEmpty(t.Titel) &&
                           (t.Titel.Equals(normalizedPayee, StringComparison.OrdinalIgnoreCase) ||
                            t.Titel.Contains(normalizedPayee, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (matchingTransactions.Count == 0)
                return null;

            // Filter to only categorized transactions (non-empty KategorieId)
            var categorizedMatches = matchingTransactions
                .Where(t => !string.IsNullOrEmpty(t.KategorieId))
                .ToList();

            if (categorizedMatches.Count == 0)
                return null;

            // Count only the categorized matches
            var categoryCounts = categorizedMatches
                .GroupBy(t => t.KategorieId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (categoryCounts.Count == 0)
                return null;

            var topCount = categoryCounts.First().Count;
            // Confidence based on ALL matching transactions (including uncategorized)
            // This gives an honest percentage of how many transactions have this category
            var confidence = (double)topCount / matchingTransactions.Count;

            // Check confidence threshold
            if (confidence < confidenceThreshold)
                return null;

            var topCategoryId = categoryCounts.First().CategoryId;

            // Honor cancellation before category load
            cancellationToken.ThrowIfCancellationRequested();

            // Load categories through CategoryStore if available (thread-safe)
            // Otherwise fall back to direct load (for testing)
            List<Category> categories;
            if (_categoryStore != null)
            {
                categories = await _categoryStore.GetCategoriesAsync();
            }
            else
            {
                var categoriesFile = Path.Combine(DataDir, DataFileNames.Categories);
                categories = await LoadAsync<Category>(categoriesFile);
            }

            // Honor cancellation after category load
            cancellationToken.ThrowIfCancellationRequested();

            var topCategory = categories.FirstOrDefault(c => c.Id == topCategoryId);

            // Don't use Unkategorisiert category
            if (topCategory != null &&
                topCategory.SystemKey != SystemCategoryKeys.Unkategorisiert &&
                topCategory.Name != "Unkategorisiert")
            {
                return topCategory;
            }

            return null;
        }
        finally
        {
            StoreLock.Release();
        }
    }
}
