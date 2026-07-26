using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// JSON-based adapter for Category repository operations.
/// Handles persistence of categories with thread-safe CRUD operations.
/// </summary>
public class CategoryStore : JsonDataStoreBase, ICategoryRepository
{
    private string CategoriesFile => Path.Combine(DataDir, DataFileNames.Categories);

    public CategoryStore(string dataDir, ILogger<CategoryStore>? logger = null)
        : base(dataDir, logger)
    {
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await StoreLock.WaitAsync();
        try
        {
            return await LoadAsync<Category>(CategoriesFile);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task SaveCategoryAsync(Category category)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Category>(CategoriesFile);
            var idx = items.FindIndex(c => c.Id == category.Id);
            if (idx >= 0)
                items[idx] = category;
            else
                items.Add(category);
            await SaveAsync(CategoriesFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task DeleteCategoryAsync(string id)
    {
        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<Category>(CategoriesFile);
            items.RemoveAll(c => c.Id == id);
            await SaveAsync(CategoriesFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public Task ReplaceAllCategoriesAsync(IEnumerable<Category> categories)
        => ReplaceAllAsync(CategoriesFile, categories);
}
