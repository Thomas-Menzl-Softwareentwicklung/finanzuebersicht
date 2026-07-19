using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

public class SparZielStore : JsonDataStoreBase, ISparZielRepository
{
    private string SparZieleFile => Path.Combine(DataDir, DataFileNames.Sparziele);

    public SparZielStore(string dataDir, ILogger<SparZielStore>? logger = null)
        : base(dataDir, logger) { }

    public async Task<List<SparZiel>> GetSparZieleAsync()
    {
        await StoreLock.WaitAsync();
        try { return await LoadAsync<SparZiel>(SparZieleFile); }
        finally { StoreLock.Release(); }
    }

    public async Task SaveSparZielAsync(SparZiel sparZiel)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<SparZiel>(SparZieleFile);
            var idx = items.FindIndex(s => s.Id == sparZiel.Id);
            if (idx >= 0) items[idx] = sparZiel; else items.Add(sparZiel);
            await SaveAsync(SparZieleFile, items);
        }
        finally { StoreLock.Release(); }
    }

    public async Task DeleteSparZielAsync(string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<SparZiel>(SparZieleFile);
            items.RemoveAll(s => s.Id == id);
            await SaveAsync(SparZieleFile, items);
        }
        finally { StoreLock.Release(); }
    }

    public Task ReplaceAllSparZieleAsync(IEnumerable<SparZiel> sparziele)
        => ReplaceAllAsync(SparZieleFile, sparziele);
}
