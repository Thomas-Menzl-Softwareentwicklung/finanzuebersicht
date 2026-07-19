using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// JSON-based adapter for RecurringTransaction repository operations.
/// Handles persistence of recurring transactions with thread-safe CRUD operations.
/// </summary>
public class RecurringStore : JsonDataStoreBase, IRecurringTransactionRepository
{
    private string RecurringFile => Path.Combine(DataDir, DataFileNames.Recurring);

    public RecurringStore(string dataDir, ILogger<RecurringStore>? logger = null)
        : base(dataDir, logger)
    {
    }

    public async Task<List<RecurringTransaction>> GetRecurringTransactionsAsync()
    {
        await StoreLock.WaitAsync();
        try
        {
            return await LoadAsync<RecurringTransaction>(RecurringFile);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task SaveRecurringTransactionAsync(RecurringTransaction recurring)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<RecurringTransaction>(RecurringFile);
            var idx = items.FindIndex(r => r.Id == recurring.Id);
            if (idx >= 0)
                items[idx] = recurring;
            else
                items.Add(recurring);
            await SaveAsync(RecurringFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task DeleteRecurringTransactionAsync(string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<RecurringTransaction>(RecurringFile);
            items.RemoveAll(r => r.Id == id);
            await SaveAsync(RecurringFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public Task ReplaceAllRecurringTransactionsAsync(IEnumerable<RecurringTransaction> recurring)
        => ReplaceAllAsync(RecurringFile, recurring);
}
