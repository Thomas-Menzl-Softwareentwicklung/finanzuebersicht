using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Core.Services;

public sealed class UncategorizedCategoryService(ICategoryRepository categoryRepository) : IUncategorizedCategoryService
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public bool IsUncategorized(Category category)
        => category.SystemKey == SystemCategoryKeys.Unkategorisiert
           || string.Equals(category.Name, "Unkategorisiert", StringComparison.OrdinalIgnoreCase);

    public async Task<string?> FindIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var categories = await _categoryRepository.GetCategoriesAsync().ConfigureAwait(false);
        return categories.FirstOrDefault(IsUncategorized)?.Id;
    }

    public async Task<string> EnsureAsync(CancellationToken cancellationToken = default)
    {
        var existingId = await FindIdAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(existingId))
            return existingId;

        var uncategorized = new Category
        {
            Name = "Unkategorisiert",
            Icon = "❓",
            Color = "#A2845E",
            Typ = TransactionType.Ausgabe,
            SystemKey = SystemCategoryKeys.Unkategorisiert
        };

        cancellationToken.ThrowIfCancellationRequested();
        await _categoryRepository.SaveCategoryAsync(uncategorized).ConfigureAwait(false);
        return uncategorized.Id;
    }
}
