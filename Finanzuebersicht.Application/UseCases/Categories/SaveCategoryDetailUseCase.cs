using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Categories;

public class SaveCategoryDetailUseCase(
    ICategoryRepository categoryRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task<Category> ExecuteAsync(
        Category? existingCategory,
        string name,
        string icon,
        string color,
        TransactionType typ, CancellationToken cancellationToken = default)
    {
        var category = existingCategory ?? new Category();
        category.Name = name;
        category.Icon = icon;
        category.Color = color;
        category.Typ = typ;
        CloudSyncNotify.StampUpdatedAt(category);

        await _categoryRepository.SaveCategoryAsync(category);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Category,
            category.Id,
            cancellationToken);
        return category;
    }
}
