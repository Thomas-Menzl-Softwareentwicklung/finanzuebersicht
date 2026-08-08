using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Categories;

public class GetCategoryByIdUseCase(ICategoryRepository categoryRepository)
{
    public async Task<Category?> ExecuteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var categories = await categoryRepository.GetCategoriesAsync();
        return categories.FirstOrDefault(c => c.Id == id);
    }
}
