using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

public class TransactionTemplateStore(string dataDir, ILogger<TransactionTemplateStore>? logger = null)
    : JsonDataStoreBase(dataDir, logger), ITransactionTemplateRepository
{
    private string TemplatesFile => Path.Combine(DataDir, DataFileNames.TransactionTemplates);

    public async Task<List<TransactionTemplate>> GetTransactionTemplatesAsync()
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<TransactionTemplate>(TemplatesFile);
            return [.. items
                .OrderByDescending(t => t.LastUsedAt ?? DateTime.MinValue)
                .ThenByDescending(t => t.UseCount)
                .ThenBy(t => t.Name)];
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task SaveTransactionTemplateAsync(TransactionTemplate template)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<TransactionTemplate>(TemplatesFile);
            var idx = items.FindIndex(t => t.Id == template.Id);
            if (idx >= 0)
                items[idx] = template;
            else
                items.Add(template);
            await SaveAsync(TemplatesFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task DeleteTransactionTemplateAsync(string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<TransactionTemplate>(TemplatesFile);
            items.RemoveAll(t => t.Id == id);
            await SaveAsync(TemplatesFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public Task ReplaceAllTransactionTemplatesAsync(IEnumerable<TransactionTemplate> templates)
        => ReplaceAllAsync(TemplatesFile, templates);
}
